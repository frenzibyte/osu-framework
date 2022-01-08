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
        /// Whether masking is currently active.
        /// </summary>
        public bool IsMaskingActive => MaskingInfo.Count > 1;

        /// <summary>
        /// The current masking info state.
        /// </summary>
        RendererState<MaskingInfo> MaskingInfo { get; }

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
        /// Resets the renderer state and performs per-frame operations.
        /// </summary>
        /// <param name="size">The size to set to the main framebuffer.</param>
        void Reset(Vector2 size);

        /// <inheritdoc cref="IGraphicsBackend.Clear"/>
        void Clear(ClearInfo clearInfo);

        /// <summary>
        /// Enqueues a texture to be uploaded in the next frame.
        /// </summary>
        /// <param name="texture">The texture to be uploaded.</param>
        void EnqueueTextureUpload(RendererTexture texture);

        /// <summary>
        /// Schedules an expensive operation to a queue from which a maximum of one operation is performed per frame.
        /// </summary>
        /// <param name="operation">The operation to schedule.</param>
        void ScheduleExpensiveOperation(ScheduledDelegate operation);

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

        /// <summary>
        /// Sets the current draw depth.
        /// The draw depth is written to every vertex added to <see cref="VertexBuffer{T}"/>s.
        /// </summary>
        /// <param name="drawDepth">The draw depth.</param>
        void SetDrawDepth(float drawDepth);

        /// <summary>
        /// Sets the blending parameters to draw with.
        /// </summary>
        void SetBlend(BlendingParameters blendingParameters);

        /// <inheritdoc cref="IGraphicsBackend.SetVertexBuffer{TIndex}"/>
        void BindVertexBuffer<TIndex>(IVertexBuffer buffer, IReadOnlyList<VertexLayoutElement> layout)
            where TIndex : unmanaged;

        /// <summary>
        /// Binds a texture to draw with.
        /// </summary>
        /// <param name="texture">The txeture to bind.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>True if the provided texture was not already bound (causing a binding change).</returns>
        bool BindTexture(RendererTexture texture, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None);

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
        /// Schedules a disposal action on the specified target.
        /// </summary>
        /// <param name="disposalAction">The disposal action.</param>
        /// <param name="target">The target to be disposed of.</param>
        void ScheduleDisposal<T>(Action<T> disposalAction, T target);
    }
}
