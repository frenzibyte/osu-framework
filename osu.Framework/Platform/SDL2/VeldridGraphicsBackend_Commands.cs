// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Renderer;
using osu.Framework.Graphics.Shaders;
using Veldrid;

namespace osu.Framework.Platform.SDL2
{
    public partial class VeldridGraphicsBackend
    {
        private static CommandList globalCommands;

        public static CommandList Commands { get; private set; }

        /// <summary>
        /// A <see cref="Fence"/> signaled when the recently submitted <see cref="Commands"/> completes execution.
        /// </summary>
        public static Fence CompletedCommandsExecution { get; private set; }

        private void initialiseCommands()
        {
            globalCommands = Factory.CreateCommandList();
            CompletedCommandsExecution = Factory.CreateFence(false);
        }

        /// <summary>
        /// Starts a sequence of commands to a <see cref="CommandList"/>.
        /// </summary>
        /// <returns>An <see cref="InvokeOnDisposal"/> to be used in a <see langword="using"/> statement.</returns>
        public static IDisposable BeginCommands() => BeginCommands(out _);

        /// <summary>
        /// Starts a sequence of commands to send to a <see cref="CommandList"/>.
        /// </summary>
        /// <param name="commands">The command list.</param>
        /// <returns>An <see cref="InvokeOnDisposal"/> to be used in a <see langword="using"/> statement.</returns>
        public static IDisposable BeginCommands(out CommandList commands)
        {
            if (Commands != null)
                throw new InvalidOperationException("A command list has already begun accepting commands.");

            Commands = commands = globalCommands;
            Commands.Begin();

            return new ValueInvokeOnDisposal<CommandList>(commands, c =>
            {
                Commands.End();
                Device.SubmitCommands(Commands, CompletedCommandsExecution);

                Commands = null;
            });
        }

        public static void DrawVertices(PrimitiveTopology topology, int verticesStart, int verticesCount)
        {
            pipelineDescription.PrimitiveTopology = topology;

            validateShaderLayout();

            Commands.SetPipeline(fetchPipeline(pipelineDescription));
            Commands.SetGraphicsResourceSet(UNIFORM_RESOURCE_SLOT, currentShader.UniformResourceSet);
            Commands.SetGraphicsResourceSet(TEXTURE_RESOURCE_SLOT, boundTextureSet);

            Commands.DrawIndexed((uint)verticesCount, 1, (uint)verticesStart, 0, 0);
        }

        private static void validateShaderLayout()
        {
            var vertexShaderLayout = pipelineDescription.ShaderSet.VertexLayouts.Single();

            if (vertexShaderLayout.Elements.Length != boundVertexLayout.Elements.Length)
                throw new VertexLayoutMismatchException(currentShader.Name, vertexShaderLayout, boundVertexLayout, $"Length mismatch ({vertexShaderLayout.Elements.Length} != {boundVertexLayout.Elements.Length}).");

            for (int i = 0; i < vertexShaderLayout.Elements.Length; i++)
            {
                var shaderElement = vertexShaderLayout.Elements[i];
                var bufferElement = boundVertexLayout.Elements[i];

                if (shaderElement.Format != bufferElement.Format)
                {
                    throw new VertexLayoutMismatchException(currentShader.Name, vertexShaderLayout, boundVertexLayout, $"Element {i - ShaderPart.BACKBUFFER_ATTRIBUTE_OFFSET} in vertex shader with format ({shaderElement.Format}) does not match corresponding element in vertex buffer layout ({bufferElement.Format}).");
                }
            }
        }

        #region Clear

        public static void Clear(ClearInfo clearInfo)
        {
            Commands.ClearColorTarget(0, clearInfo.Colour.ToRgbaFloat());

            if (frame_buffer_stack.Peek().DepthTarget != null)
                Commands.ClearDepthStencil((float)clearInfo.Depth, (byte)clearInfo.Stencil);
        }

        #endregion

        #region Viewport

        private static readonly Stack<RectangleI> viewport_stack = new Stack<RectangleI>();

        /// <summary>
        /// Applies a new viewport rectangle.
        /// </summary>
        /// <param name="viewport">The viewport rectangle.</param>
        public static void PushViewport(RectangleI viewport)
        {
            var actualRect = viewport;

            if (actualRect.Width < 0)
            {
                actualRect.X += viewport.Width;
                actualRect.Width = -viewport.Width;
            }

            if (actualRect.Height < 0)
            {
                actualRect.Y += viewport.Height;
                actualRect.Height = -viewport.Height;
            }

            PushOrtho(viewport);

            viewport_stack.Push(actualRect);

            if (Viewport == actualRect)
                return;

            Viewport = actualRect;

            Commands.SetViewport(0, new Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height, Device.IsDepthRangeZeroToOne ? 0 : -1, 1));
        }

        /// <summary>
        /// Applies the last viewport rectangle.
        /// </summary>
        public static void PopViewport()
        {
            Trace.Assert(viewport_stack.Count > 1);

            PopOrtho();

            viewport_stack.Pop();
            RectangleI actualRect = viewport_stack.Peek();

            if (Viewport == actualRect)
                return;

            Viewport = actualRect;

            Commands.SetViewport(0, new Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height, Device.IsDepthRangeZeroToOne ? 0 : -1, 1));
        }

        #endregion

        #region Scissor

        /// <summary>
        /// Applies a new scissor rectangle.
        /// </summary>
        /// <param name="scissor">The scissor rectangle.</param>
        public static void PushScissor(RectangleI scissor)
        {
            FlushCurrentBatch();

            scissor_rect_stack.Push(scissor);
            if (Scissor == scissor)
                return;

            Scissor = scissor;
            setScissor(scissor);
        }

        /// <summary>
        /// Applies the last scissor rectangle.
        /// </summary>
        public static void PopScissor()
        {
            Trace.Assert(scissor_rect_stack.Count > 1);

            FlushCurrentBatch();

            scissor_rect_stack.Pop();
            RectangleI scissor = scissor_rect_stack.Peek();

            if (Scissor == scissor)
                return;

            Scissor = scissor;
            setScissor(scissor);
        }

        private static void setScissor(RectangleI scissor)
        {
            if (scissor.Width < 0)
            {
                scissor.X += scissor.Width;
                scissor.Width = -scissor.Width;
            }

            if (scissor.Height < 0)
            {
                scissor.Y += scissor.Height;
                scissor.Height = -scissor.Height;
            }

            Commands.SetScissorRect(0, (uint)scissor.X, (uint)(Viewport.Height - scissor.Bottom), (uint)scissor.Width, (uint)scissor.Height);
        }

        #endregion

        #region Framebuffer

        /// <summary>
        /// Binds a framebuffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer to bind.</param>
        public static void BindFrameBuffer(Framebuffer frameBuffer)
        {
            if (frameBuffer == null) return;

            bool alreadyBound = frame_buffer_stack.Count > 0 && frame_buffer_stack.Peek() == frameBuffer;

            frame_buffer_stack.Push(frameBuffer);

            if (!alreadyBound)
            {
                FlushCurrentBatch();

                Commands.SetFramebuffer(frameBuffer);
                pipelineDescription.Outputs = frameBuffer.OutputDescription;

                GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            }

            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        /// <summary>
        /// Unbinds a framebuffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer to unbind.</param>
        public static void UnbindFrameBuffer(Framebuffer frameBuffer)
        {
            if (frameBuffer == null) return;

            if (frame_buffer_stack.Peek() != frameBuffer)
                return;

            frame_buffer_stack.Pop();

            FlushCurrentBatch();

            Commands.SetFramebuffer(frame_buffer_stack.Peek());
            pipelineDescription.Outputs = frame_buffer_stack.Peek().OutputDescription;

            GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        #endregion

        private class VertexLayoutMismatchException : Exception
        {
            public VertexLayoutMismatchException(string shaderName, VertexLayoutDescription shaderLayout, VertexLayoutDescription bufferLayout, string message)
                : base($"Vertex input layout mismatch between bound shader '{shaderName}' ({getDisplayString(shaderLayout)}) and bound vertex buffer ({getDisplayString(bufferLayout)}): {message}")
            {
            }

            private static string getDisplayString(VertexLayoutDescription layout) => string.Join(", ", layout.Elements.Skip(ShaderPart.BACKBUFFER_ATTRIBUTE_OFFSET).Select(l => l.Format));
        }
    }
}
