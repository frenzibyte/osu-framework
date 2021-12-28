// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Renderer;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Statistics;
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
        public static Fence SubmittedCommandsCompletion { get; private set; }

        private void initialiseCommands()
        {
            globalCommands = Factory.CreateCommandList();
            SubmittedCommandsCompletion = Factory.CreateFence(false);
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
            Commands = commands = globalCommands;
            Commands.Begin();

            return new ValueInvokeOnDisposal<CommandList>(commands, c =>
            {
                Commands.End();
                Device.SubmitCommands(Commands, SubmittedCommandsCompletion);

                Commands = null;
            });
        }

        public static void UpdateBuffer<T>(DeviceBuffer buffer, int offset, ref T value, int? size = null)
            where T : struct, IEquatable<T>
        {
            size ??= Marshal.SizeOf<T>();

            var staging = VeldridStagingBufferPool.Get(size.Value);
            Device.UpdateBuffer(staging, 0, ref value, (uint)size);
            Commands.CopyBuffer(staging, 0, buffer, (uint)offset, (uint)size);
        }

        public static unsafe void UpdateTexture<T>(Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data)
            where T : unmanaged
        {
            var staging = VeldridStagingTexturePool.Get(width, height, texture.Format);

            fixed (T* ptr = data)
                Device.UpdateTexture(staging, (IntPtr)ptr, (uint)(data.Length * sizeof(T)), 0, 0, 0, (uint)width, (uint)height, 1, 0, 0);

            Commands.CopyTexture(staging, 0, 0, 0, 0, 0, texture, (uint)x, (uint)y, 0, (uint)level, 0, (uint)width, (uint)height, 1, 1);
        }

        private static ClearInfo currentClearInfo;

        public static void Clear(ClearInfo clearInfo)
        {
            PushDepthInfo(new DepthInfo(writeDepth: true));
            PushScissorState(false);

            if (clearInfo.Colour != currentClearInfo.Colour)
                Commands.ClearColorTarget(0, clearInfo.Colour.ToRgbaFloat());

            if (frame_buffer_stack.Peek().DepthTarget != null)
            {
                if (clearInfo.Depth != currentClearInfo.Depth || clearInfo.Stencil != currentClearInfo.Stencil)
                    Commands.ClearDepthStencil((float)clearInfo.Depth, (byte)clearInfo.Stencil);
            }

            currentClearInfo = clearInfo;

            PopScissorState();
            PopDepthInfo();
        }

        private static DeviceBuffer boundVertexBuffer;

        public static bool BindVertexBuffer(DeviceBuffer buffer, VertexLayoutDescription layout)
        {
            if (buffer == boundVertexBuffer)
                return false;

            Commands.SetVertexBuffer(0, buffer);

            pipelineDescription.ShaderSet.VertexLayouts = new[] { layout };

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);

            boundVertexBuffer = buffer;
            return true;
        }

        public static void BindIndexBuffer(DeviceBuffer buffer, IndexFormat format) => Commands.SetIndexBuffer(buffer, format);

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

            // TODO: depth might need to be -1 to 1 rather than 0 to 1, idk
            Commands.SetViewport(0, new Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height, 0, 1));
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

            // TODO: depth might need to be -1 to 1 rather than 0 to 1, idk
            Commands.SetViewport(0, new Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height, 0, 1));
        }

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

        public static void DrawVertices(PrimitiveTopology topology, int verticesStart, int verticesCount)
        {
            pipelineDescription.PrimitiveTopology = topology;

            Commands.SetPipeline(fetchPipeline(pipelineDescription));
            Commands.SetGraphicsResourceSet(UNIFORM_RESOURCE_SLOT, currentShader.UniformResourceSet);
            Commands.SetGraphicsResourceSet(TEXTURE_RESOURCE_SLOT, boundTextureSet);

            Commands.DrawIndexed((uint)verticesCount, 1, (uint)verticesStart, 0, 0);
        }

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

        /// <summary>
        /// Deletes a frame buffer.
        /// </summary>
        /// <param name="frameBuffer">The frame buffer to delete.</param>
        internal static void DeleteFrameBuffer(Framebuffer frameBuffer)
        {
            if (frameBuffer == null) return;

            while (frame_buffer_stack.Peek() == frameBuffer)
                UnbindFrameBuffer(frameBuffer);

            ScheduleDisposal(f => f.Dispose(), frameBuffer);
        }
    }
}
