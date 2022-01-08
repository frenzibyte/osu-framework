// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Buffers;
using osu.Framework.Graphics.Rendering.Textures;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osuTK;
using Veldrid;
using PrimitiveTopology = osu.Framework.Graphics.Rendering.PrimitiveTopology;
using Shader = osu.Framework.Graphics.Shaders.Shader;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Provides an implementation-agnostic interface on the backing low-level graphics API.
    /// </summary>
    public interface IGraphicsBackend
    {
        /// <summary>
        /// Invoked when a <see cref="SwapBuffers"/> call is performed.
        /// </summary>
        [CanBeNull]
        event Action OnSwap;

        /// <summary>
        /// The graphics backend type.
        /// </summary>
        GraphicsBackend Type { get; }

        /// <summary>
        /// The graphics backend factory.
        /// </summary>
        IGraphicsFactory Factory { get; }

        /// <summary>
        /// The blending parameters state currently applied to the backend.
        /// </summary>
        BlendingParameters BlendingParameters { set; }

        /// <summary>
        /// The depth information state currently applied to the backend.
        /// </summary>
        DepthInfo DepthInfo { set; }

        /// <summary>
        /// Whether scissor testing should be enabled in the backend.
        /// </summary>
        bool ScissorTest { set; }

        /// <summary>
        /// Whether buffer swapping should be synced to the monitor's refresh rate.
        /// </summary>
        bool VerticalSync { get; set; }

        /// <summary>
        /// Makes the graphics backend the current context, if appropriate for the driver.
        /// </summary>
        void MakeCurrent();

        /// <summary>
        /// Clears the current context, if appropriate for the driver.
        /// </summary>
        void ClearCurrent();

        /// <summary>
        /// Begins a new draw session.
        /// </summary>
        void BeginDraw();

        /// <summary>
        /// Ends the current draw session and submits it to the GPU.
        /// </summary>
        void EndDraw();

        /// <summary>
        /// Resizes the main framebuffer to the specified size.
        /// </summary>
        void Resize(Vector2 size);

        /// <summary>
        /// Performs a backbuffer swap immediately if <see cref="VerticalSync"/> is false, or on the next screen refresh if true.
        /// </summary>
        void SwapBuffers();

        /// <summary>
        /// Clears the render and depth-stencil targets of the active framebuffer.
        /// </summary>
        void Clear(ClearInfo clearInfo);

        /// <summary>
        /// Sets the viewport rectangle of the active framebuffer.
        /// </summary>
        /// <param name="position">The position of the viewport rectangle.</param>
        /// <param name="size">The size of the viewport rectangle.</param>
        void SetViewport(RectangleI viewport);

        /// <summary>
        /// Sets the scissor rectangle of the active framebuffer.
        /// </summary>
        void SetScissor(RectangleI rectangle);

        /// <summary>
        /// Sets the active framebuffer which will be rendered to.
        /// </summary>
        /// <param name="framebuffer">The framebuffer, or null to set the main framebuffer.</param>
        void SetFrameBuffer([CanBeNull] FrameBuffer framebuffer);

        /// <summary>
        /// Sets the active vertex and index buffers to draw with.
        /// </summary>
        /// <param name="buffer">The vertex buffer.</param>
        /// <param name="layout">The list of elements to define the structure layout of this vertex.</param>
        /// <typeparam name="TIndex">The index type.</typeparam>
        void SetVertexBuffer<TIndex>(IVertexBuffer buffer, IReadOnlyList<VertexLayoutElement> layout)
            where TIndex : unmanaged;

        /// <summary>
        /// Sets the active texture to draw with.
        /// </summary>
        void SetTexture(RendererTexture texture);

        /// <summary>
        /// Sets the active shader to draw with.
        /// </summary>
        void SetShader(Shader shader);

        /// <summary>
        /// Updates the vertex buffer with <paramref name="data"/> at the specified location.
        /// </summary>
        /// <param name="buffer">The vertex buffer to update.</param>
        /// <param name="start">The vertex number to begin updating from.</param>
        /// <param name="data">The new vertices data.</param>
        /// <typeparam name="T">The vertex type.</typeparam>
        void UpdateVertexBuffer<T>(IVertexBuffer buffer, int start, Memory<T> data) where T : unmanaged, IEquatable<T>, IVertex;

        /// <summary>
        /// Updates the contents of a <see cref="RendererTexture"/> with <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="RendererTexture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The textural data.</param>
        /// <typeparam name="TPixel">The pixel type.</typeparam>
        void UpdateTexture<TPixel>(RendererTexture texture, int x, int y, int width, int height, int level, Memory<TPixel> data) where TPixel : unmanaged;

        /// <summary>
        /// Updates the uniform value on the active uniform buffer.
        /// </summary>
        void UpdateUniform<T>(IUniform<T> uniform) where T : unmanaged, IEquatable<T>;

        /// <summary>
        /// Updates all uniform values of a shader on the active uniform buffer.
        /// </summary>
        /// <param name="shader">The shader whose uniforms should be updated.</param>
        void UpdateUniforms(Shader shader);

        /// <summary>
        /// Draws a number of primitives from the specified vertices.
        /// </summary>
        /// <param name="topology">The primitive topology to use for this draw call.</param>
        /// <param name="start">The index start to begin drawing from.</param>
        /// <param name="count">The number of indices to draw.</param>
        void Draw(PrimitiveTopology topology, int start, int count);

        /// <summary>
        /// Waits until GPU has finished work with the latest submitted frame.
        /// </summary>
        void WaitUntilFinished();
    }
}
