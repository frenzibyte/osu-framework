// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Buffers;
using osu.Framework.Graphics.Rendering.Textures;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osuTK;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Provides a renderer context to send commands to.
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// Maximum number of <see cref="DrawNode"/>s a <see cref="Drawable"/> can draw with.
        /// This is a carefully-chosen number to enable the update and draw threads to work concurrently without causing unnecessary load.
        /// </summary>
        const int MAX_DRAW_NODES = 3;

        /// <summary>
        /// The amount of times <see cref="Reset"/> has been invoked.
        /// </summary>
        ulong ResetId { get; }

        /// <summary>
        /// The current masking info state.
        /// </summary>
        RendererState<MaskingInfo> MaskingInfo { get; }

        /// <summary>
        /// Whether masking is currently active.
        /// </summary>
        public bool IsMaskingActive => MaskingInfo.Count > 1;

        /// <summary>
        /// The current depth info state.
        /// </summary>
        RendererState<DepthInfo> DepthInfo { get; }

        /// <summary>
        /// The current viewport rectangle state.
        /// </summary>
        RendererState<RectangleI> Viewport { get; }

        /// <summary>
        /// The current orthographic rectangle state.
        /// </summary>
        RendererState<RectangleF> Ortho { get; }

        /// <summary>
        /// The current scissor rectangle state.
        /// </summary>
        RendererState<RectangleI> Scissor { get; }

        /// <summary>
        /// The current scissor rectangle offset state.
        /// </summary>
        RendererState<Vector2I> ScissorOffset { get; }

        /// <summary>
        /// The current blending parameters state.
        /// </summary>
        BlendingParameters BlendingParameters { get; set; }

        /// <summary>
        /// The current draw depth.
        /// </summary>
        /// <remarks>
        /// This is written to every vertex added to the <see cref="VertexBuffer{T}"/>s.
        /// </remarks>
        float DrawDepth { get; set; }

        /// <summary>
        /// The current texture wrap mode in horizontal direction.
        /// </summary>
        WrapMode CurrentWrapModeS { get; }

        /// <summary>
        /// The current texture wrap mode in vertical direction.
        /// </summary>
        WrapMode CurrentWrapModeT { get; }

        /// <summary>
        /// Whether the currently bound texture is an atlas.
        /// </summary>
        bool AtlasTextureIsBound { get; }

        /// <summary>
        /// Resets the renderer state and performs per-frame operations.
        /// </summary>
        /// <param name="size">The size to set to the main framebuffer.</param>
        void Reset(Vector2 size);

        /// <inheritdoc cref="IGraphicsBackend.Clear"/>
        void Clear(ClearInfo clearInfo);

        /// <summary>
        /// Schedules an expensive operation to a queue from which a maximum of one operation is performed per frame.
        /// </summary>
        /// <param name="operation">The operation to schedule.</param>
        void ScheduleExpensiveOperation(ScheduledDelegate operation);

        /// <summary>
        /// Enqueues a texture to be uploaded in the next frame.
        /// </summary>
        /// <param name="texture">The texture to be uploaded.</param>
        void EnqueueTextureUpload(RendererTexture texture);

        /// <summary>
        /// Sets the current vertex batch used for drawing.
        /// <para>
        /// This is done so that various methods that change renderer state can force-draw the batch
        /// before continuing with the state change.
        /// </para>
        /// </summary>
        /// <param name="batch">The batch.</param>
        void SetActiveBatch(IVertexBatch batch);

        /// <summary>
        /// Notifies that an <see cref="IVertexBuffer"/> has begun being used.
        /// </summary>
        /// <param name="buffer">The <see cref="IVertexBuffer"/> in use.</param>
        void RegisterVertexBufferUse(IVertexBuffer buffer);

        #region Factory

        /// <summary>
        /// Creates a new texture.
        /// </summary>
        /// <param name="width">The texture width.</param>
        /// <param name="height">The texture height.</param>
        /// <param name="format">The texture pixel format.</param>
        /// <param name="maximumLevels">The maximum number of mipmap levels.</param>
        /// <param name="returnValue">An action to return the created texture at time of execution.</param>
        void CreateTexture(int width, int height, PixelFormat format, int maximumLevels, Action<IDisposable> returnValue);

        /// <summary>
        /// Creates a new vertex buffer.
        /// </summary>
        /// <param name="length">The buffer length in bytes.</param>
        /// <param name="returnValue">An action to return the created vertex buffer at time of execution.</param>
        void CreateVertexBuffer(int length, Action<IDisposable> returnValue);

        /// <summary>
        /// Creates a new index buffer.
        /// </summary>
        /// <param name="length">The buffer length in bytes.</param>
        /// <param name="returnValue">An action to return the created index buffer at time of execution.</param>
        void CreateIndexBuffer(int length, Action<IDisposable> returnValue);

        /// <summary>
        /// Creates a new vertex and fragment shader.
        /// </summary>
        /// <param name="vertexBytes">The vertex shader bytes.</param>
        /// <param name="fragmentBytes">The fragment shader bytes.</param>
        /// <param name="returnValue">An action to return the created shaders and the generated vertex layout at time of execution.</param>
        void CreateVertexFragmentShaders(byte[] vertexBytes, byte[] fragmentBytes, Action<IDisposable[], IReadOnlyList<VertexLayoutElement>> returnValue);

        /// <summary>
        /// Creates a new framebuffer.
        /// </summary>
        /// <param name="target">The main render target.</param>
        /// <param name="renderFormats">The render formats of the framebuffer.</param>
        /// <param name="depthFormat">The depth format of the framebuffer, or null to disable depth on this framebuffer.</param>
        /// <param name="returnValue">An action to return the created frame buffer at time of execution.</param>
        void CreateFrameBuffer(RendererTexture target, PixelFormat[] renderFormats, PixelFormat? depthFormat, Action<IDisposable> returnValue);

        #endregion

        #region Encoding

        /// <summary>
        /// Updates the vertex buffer with <paramref name="data"/> at the specified location.
        /// </summary>
        /// <param name="buffer">The vertex buffer to update.</param>
        /// <param name="start">The vertex number to begin updating from.</param>
        /// <param name="data">The new vertices data.</param>
        /// <typeparam name="T">The vertex type.</typeparam>
        void UpdateVertexBuffer<T>(IVertexBuffer buffer, int start, Memory<T> data)
            where T : unmanaged, IEquatable<T>, IVertex;

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
        void UpdateTexture<TPixel>(RendererTexture texture, int x, int y, int width, int height, int level, Memory<TPixel> data)
            where TPixel : unmanaged;

        /// <summary>
        /// Updates the uniform value on the active uniform buffer.
        /// </summary>
        void UpdateUniform<T>(IUniform<T> uniform) where T : unmanaged, IEquatable<T>;

        /// <summary>
        /// Updates all uniform values of a shader on the active uniform buffer.
        /// </summary>
        /// <param name="shader">The shader whose uniforms should be updated.</param>
        void UpdateUniforms(Shader shader);

        #endregion

        #region Binding

        /// <inheritdoc cref="IGraphicsBackend.SetVertexBuffer{TIndex}"/>
        void BindVertexBuffer<TIndex>(IVertexBuffer buffer, IReadOnlyList<VertexLayoutElement> layout)
            where TIndex : unmanaged;

        /// <summary>
        /// Binds a texture to draw with.
        /// </summary>
        /// <param name="texture">The txeture to bind.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <param name="onBound">Invoked when the texture has been bound successfully and not already bound.</param>
        void BindTexture(RendererTexture texture, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None, Action onBound = null);

        /// <summary>
        /// Binds a shader to draw with.
        /// </summary>
        /// <param name="shader">The shader to bind.</param>
        void BindShader(Shader shader);

        /// <summary>
        /// Binds a frame buffer.
        /// </summary>
        /// <param name="frameBuffer">The frame buffer to bind.</param>
        void BindFrameBuffer(FrameBuffer frameBuffer);

        /// <summary>
        /// Unbinds a frame buffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer to unbind.</param>
        void UnbindFrameBuffer(FrameBuffer frameBuffer);

        #endregion

        /// <summary>
        /// Schedules a disposal action on the specified target.
        /// </summary>
        /// <param name="disposalAction">The disposal action.</param>
        /// <param name="target">The target to be disposed of.</param>
        void ScheduleDisposal<T>(Action<T> disposalAction, T target);
    }
}
