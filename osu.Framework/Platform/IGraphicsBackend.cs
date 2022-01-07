// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
using Shader = osu.Framework.Graphics.Shaders.Shader;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Provides an implementation-agnostic interface on the backing low-level graphics API.
    /// </summary>
    public interface IGraphicsBackend
    {
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
        /// Performs a backbuffer swap immediately if <see cref="VerticalSync"/> is false, or on the next screen refresh if true.
        /// </summary>
        void SwapBuffers();

        /// <summary>
        /// Queues an indexed draw call to the active framebuffer at the specified range in the indices buffer.
        /// </summary>
        /// <param name="start">The index start to begin drawing from.</param>
        /// <param name="count">The number of indices to draw.</param>
        void DrawVertices(int start, int count);

        /// <summary>
        /// Clears the render and depth-stencil targets of the active framebuffer.
        /// </summary>
        void Clear(ClearInfo clearInfo);

        /// <summary>
        /// Sets the viewport rectangle of the active framebuffer.
        /// </summary>
        /// <param name="position">The position of the viewport rectangle.</param>
        /// <param name="size">The size of the viewport rectangle.</param>
        void SetViewport(Vector3 position, Vector3 size);

        /// <summary>
        /// Sets the scissor rectangle of the active framebuffer.
        /// </summary>
        void SetScissor(RectangleI rectangle);

        /// <summary>
        /// Sets the active framebuffer which will be rendered to.
        /// </summary>
        /// <param name="framebuffer">The framebuffer, or null to set the main framebuffer.</param>
        void SetFramebuffer([CanBeNull] FrameBuffer framebuffer);

        /// <summary>
        /// Sets the active vertex and index buffers to draw with.
        /// </summary>
        /// <typeparam name="T">The vertex type.</typeparam>
        /// <typeparam name="TIndex">The index type.</typeparam>
        void SetVertexBuffer<T, TIndex>(VertexBuffer<T> buffer)
            where T : unmanaged, IEquatable<T>, IVertex
            where TIndex : unmanaged;

        /// <summary>
        /// Updates the vertex buffer with <paramref name="data"/> at the specified location.
        /// </summary>
        /// <param name="buffer">The vertex buffer to update.</param>
        /// <param name="start">The vertex number to begin updating from.</param>
        /// <param name="data">The new vertices data.</param>
        /// <typeparam name="T">The vertex type.</typeparam>
        void UpdateVertexBuffer<T>(VertexBuffer<T> buffer, int start, Memory<T> data) where T : unmanaged, IEquatable<T>, IVertex;

        /// <summary>
        /// Sets the active texture to draw with.
        /// </summary>
        void SetTexture(RendererTexture texture);

        /// <summary>
        /// Updates the contents of a <see cref="Texture"/> with <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The textural data.</param>
        /// <typeparam name="TPixel">The pixel type.</typeparam>
        void UpdateTexture<TPixel>(RendererTexture texture, int x, int y, int width, int height, int level, ReadOnlySpan<TPixel> data) where TPixel : unmanaged;

        /// <summary>
        /// Sets the active shader to draw with.
        /// </summary>
        void SetShader(Shader shader);

        /// <summary>
        /// Updates the uniform value on the active uniform buffer.
        /// </summary>
        void UpdateUniform<T>(IUniform<T> uniform) where T : unmanaged, IEquatable<T>;
    }
}
