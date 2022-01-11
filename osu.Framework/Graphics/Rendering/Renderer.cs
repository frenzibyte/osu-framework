// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Development;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Buffers;
using osu.Framework.Graphics.Rendering.Textures;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osuTK;
using static osu.Framework.Threading.ScheduledDelegate;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// The managed game renderer backed by an <see cref="IGraphicsBackend"/>.
    /// </summary>
    public class Renderer : IRenderer
    {
        /// <summary>
        /// The interval (in frames) before checking whether device resources should be freed.
        /// VBOs may remain unused for at most double this length before they are recycled.
        /// </summary>
        private const int resources_free_check_interval = 300;

        /// <summary>
        /// The amount of times <see cref="Reset"/> has been invoked.
        /// </summary>
        public ulong ResetId { get; private set; }

        // todo: access value from graphics backend
        public int MaxTextureSize { get; } = 4096;

        /// <summary>
        /// The maximum number of texture uploads to dequeue and upload per frame.
        /// Defaults to 32.
        /// </summary>
        public int MaxTexturesUploadedPerFrame { get; set; } = 32;

        /// <summary>
        /// The maximum number of pixels to upload per frame.
        /// Defaults to 2 megapixels (8mb alloc).
        /// </summary>
        public int MaxPixelsUploadedPerFrame { get; set; } = 1024 * 1024 * 2;

        private readonly GameHost host;
        private readonly IGraphicsBackend graphicsBackend;

        public RendererState<MaskingInfo> MaskingInfo { get; }
        public RendererState<DepthInfo> DepthInfo { get; }
        public RendererState<RectangleI> Viewport { get; }
        public RendererState<RectangleF> Ortho { get; }
        public RendererState<RectangleI> Scissor { get; }
        public RendererState<bool> ScissorState { get; }
        public RendererState<Vector2I> ScissorOffset { get; }

        private BlendingParameters blendingParameters;

        public BlendingParameters BlendingParameters
        {
            get => blendingParameters;
            set
            {
                if (value == blendingParameters)
                    return;

                flushCurrentBatch();
                graphicsBackend.BlendingParameters = blendingParameters = value;
            }
        }

        public float DrawDepth { get; set; }

        private IReadOnlyList<VertexLayoutElement> boundVertexLayout;

        private RendererTexture boundTexture;

        public WrapMode CurrentWrapModeS { get; private set; }
        public WrapMode CurrentWrapModeT { get; private set; }
        public bool AtlasTextureIsBound { get; private set; }

        private readonly Stack<Shader> shaderStack = new Stack<Shader>();

        private readonly Stack<FrameBuffer> frameBufferStack = new Stack<FrameBuffer>();

        public bool UsingBackbuffer => frameBufferStack.Peek() == null;

        private readonly Scheduler resetScheduler = new Scheduler(() => ThreadSafety.IsDrawThread, new StopwatchClock(true)); // force no thread set until we are actually on the draw thread.

        /// <summary>
        /// A queue from which a maximum of one operation is invoked per draw frame.
        /// </summary>
        private readonly ConcurrentQueue<ScheduledDelegate> expensiveOperationQueue = new ConcurrentQueue<ScheduledDelegate>();

        private readonly ConcurrentQueue<RendererTexture> textureUploadQueue = new ConcurrentQueue<RendererTexture>();

        private IVertexBatch currentActiveBatch;

        private readonly List<IVertexBatch> batchResetList = new List<IVertexBatch>();
        private readonly List<IVertexBuffer> vertexBuffersInUse = new List<IVertexBuffer>();

        // private readonly RendererFencePool commandsExecutionFencePool = new RendererFencePool();
        // private readonly RendererStagingBufferPool stagingBufferPool = new RendererStagingBufferPool();
        // private readonly RendererStagingTexturePool stagingTexturePool = new RendererStagingTexturePool();

        private readonly RendererDisposalQueue disposalQueue = new RendererDisposalQueue();

        public Renderer(GameHost host, IGraphicsBackend graphicsBackend)
        {
            this.host = host;
            this.graphicsBackend = graphicsBackend;

            MaskingInfo = new RendererState<MaskingInfo>(setMaskingInfo);
            DepthInfo = new RendererState<DepthInfo>(setDepthInfo);
            Viewport = new RendererState<RectangleI>(setViewport);
            Ortho = new RendererState<RectangleF>(setOrtho);
            Scissor = new RendererState<RectangleI>(setScissor);
            ScissorState = new RendererState<bool>(setScissorState);

            ScissorOffset = new RendererState<Vector2I>((_, __) => flushCurrentBatch());

            resetScheduler.AddDelayed(() => disposalQueue.CheckPendingDisposals(), 0, true);
        }

        private static readonly GlobalStatistic<int> stat_expensive_operations_queued = GlobalStatistics.Get<int>(nameof(Renderer), "Expensive operation queue length");
        private static readonly GlobalStatistic<int> stat_texture_uploads_queued = GlobalStatistics.Get<int>(nameof(Renderer), "Texture upload queue length");
        private static readonly GlobalStatistic<int> stat_texture_uploads_dequeued = GlobalStatistics.Get<int>(nameof(Renderer), "Texture uploads dequeued");
        private static readonly GlobalStatistic<int> stat_texture_uploads_performed = GlobalStatistics.Get<int>(nameof(Renderer), "Texture uploads performed");

        private Vector2 currentSize;

        public void Reset(Vector2 size)
        {
            ResetId++;

            Trace.Assert(shaderStack.Count == 0);

            resetScheduler.Update();

            stat_expensive_operations_queued.Value = expensiveOperationQueue.Count;

            while (expensiveOperationQueue.TryDequeue(out ScheduledDelegate operation))
            {
                if (operation.State == RunState.Waiting)
                {
                    operation.RunTask();
                    break;
                }
            }

            currentActiveBatch = null;

            blendingParameters = default;

            foreach (var b in batchResetList)
                b.ResetCounters();

            batchResetList.Clear();

            Viewport.Clear();
            Ortho.Clear();
            MaskingInfo.Clear();
            Scissor.Clear();
            DepthInfo.Clear();
            ScissorState.Clear();
            ScissorOffset.Clear();

            frameBufferStack.Clear();

            if (size != currentSize)
            {
                graphicsBackend.Resize(size);
                currentSize = size;
            }

            BindFrameBuffer(null);

            ScissorState.Push(true);
            Viewport.Push(new RectangleI(0, 0, (int)size.X, (int)size.Y));
            Scissor.Push(new RectangleI(0, 0, (int)size.X, (int)size.Y));
            ScissorOffset.Push(Vector2I.Zero);
            MaskingInfo.Push(new MaskingInfo
            {
                ScreenSpaceAABB = new RectangleI(0, 0, (int)size.X, (int)size.Y),
                MaskingRect = new RectangleF(0, 0, size.X, size.Y),
                ToMaskingSpace = Matrix3.Identity,
                BlendRange = 1,
                AlphaExponent = 1,
                CornerExponent = 2.5f,
            });

            DepthInfo.Push(Rendering.DepthInfo.Default);
            Clear(new ClearInfo(Colour4.Black));

            freeUnusedResources();
            // releaseUsedResources();

            stat_texture_uploads_queued.Value = textureUploadQueue.Count;
            stat_texture_uploads_dequeued.Value = 0;
            stat_texture_uploads_performed.Value = 0;

            // increase the number of items processed with the queue length to ensure it doesn't get out of hand.
            int targetUploads = Math.Clamp(textureUploadQueue.Count / 2, 1, MaxTexturesUploadedPerFrame);
            int uploads = 0;
            int uploadedPixels = 0;

            // continue attempting to upload textures until enough uploads have been performed.
            while (textureUploadQueue.TryDequeue(out RendererTexture texture))
            {
                stat_texture_uploads_dequeued.Value++;

                texture.IsQueuedForUpload = false;

                if (!texture.Upload())
                    continue;

                stat_texture_uploads_performed.Value++;

                if (++uploads >= targetUploads)
                    break;

                if ((uploadedPixels += texture.Width * texture.Height) > MaxPixelsUploadedPerFrame)
                    break;
            }
        }

        public void Clear(ClearInfo clearInfo) => graphicsBackend.Clear(clearInfo);

        public void ScheduleExpensiveOperation(ScheduledDelegate operation) => expensiveOperationQueue.Enqueue(operation);

        public void EnqueueTextureUpload(RendererTexture texture)
        {
            if (texture.IsQueuedForUpload)
                return;

            if (host != null)
            {
                texture.IsQueuedForUpload = true;
                textureUploadQueue.Enqueue(texture);
            }
        }

        public void SetActiveBatch(IVertexBatch batch)
        {
            if (currentActiveBatch == batch)
                return;

            batchResetList.Add(batch);

            flushCurrentBatch();

            currentActiveBatch = batch;
        }

        public void RegisterVertexBufferUse(IVertexBuffer buffer) => vertexBuffersInUse.Add(buffer);

        #region Factory

        public void CreateTexture(int width, int height, PixelFormat format, int maximumLevels, Action<IDisposable> returnValue)
            => returnValue(graphicsBackend.Factory.CreateTexture(width, height, format, maximumLevels));

        public void CreateVertexBuffer(int length, Action<IDisposable> returnValue)
            => returnValue(graphicsBackend.Factory.CreateVertexBuffer(length));

        public void CreateIndexBuffer(int length, Action<IDisposable> returnValue)
            => returnValue(graphicsBackend.Factory.CreateIndexBuffer(length));

        public void CreateVertexFragmentShaders(byte[] vertexBytes, byte[] fragmentBytes, Action<IDisposable[], IReadOnlyList<VertexLayoutElement>> returnValue)
            => returnValue(graphicsBackend.Factory.CreateVertexFragmentShaders(vertexBytes, fragmentBytes, out var elements), elements);

        public void CreateFrameBuffer(RendererTexture target, PixelFormat[] renderFormats, PixelFormat? depthFormat, Action<IDisposable> returnValue)
            => returnValue(graphicsBackend.Factory.CreateFrameBuffer(target, renderFormats, depthFormat));

        #endregion

        #region Encoding

        public void UpdateVertexBuffer<T>(IVertexBuffer buffer, int start, Memory<T> data)
            where T : unmanaged, IEquatable<T>, IVertex
            => graphicsBackend.UpdateVertexBuffer(buffer, start, data);

        public void UpdateTexture<TPixel>(RendererTexture texture, int x, int y, int width, int height, int level, Memory<TPixel> data)
            where TPixel : unmanaged
            => graphicsBackend.UpdateTexture(texture, x, y, width, height, level, data);

        public void UpdateUniform<T>(IUniform<T> uniform)
            where T : unmanaged, IEquatable<T>
        {
            if (shaderStack.Count > 0 && uniform.Owner == shaderStack.Peek())
                flushCurrentBatch();

            graphicsBackend.UpdateUniform(uniform);
        }

        public void UpdateUniforms(Shader shader)
        {
            if (shaderStack.Count > 0 && shader == shaderStack.Peek())
                flushCurrentBatch();

            graphicsBackend.UpdateUniforms(shader);
        }

        #endregion

        #region Binding

        public void BindVertexBuffer<TIndex>(IVertexBuffer buffer, IReadOnlyList<VertexLayoutElement> layout)
            where TIndex : unmanaged
        {
            graphicsBackend.SetVertexBuffer<TIndex>(buffer, layout);
            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);

            boundVertexLayout = layout;
        }

        public void BindTexture(RendererTexture texture, WrapMode wrapModeS, WrapMode wrapModeT, Action onBound)
        {
            if (wrapModeS != CurrentWrapModeS)
            {
                GlobalPropertyManager.Set(GlobalProperty.WrapModeS, (int)wrapModeS);
                CurrentWrapModeS = wrapModeS;
            }

            if (wrapModeT != CurrentWrapModeT)
            {
                GlobalPropertyManager.Set(GlobalProperty.WrapModeT, (int)wrapModeT);
                CurrentWrapModeT = wrapModeT;
            }

            if (boundTexture == texture)
                return;

            flushCurrentBatch();

            graphicsBackend.SetTexture(texture);

            boundTexture = texture;
            AtlasTextureIsBound = false;

            FrameStatistics.Increment(StatisticsCounterType.TextureBinds);
            onBound?.Invoke();
        }

        public void BindShader(Shader shader)
        {
            ThreadSafety.EnsureDrawThread();

            var lastShader = shaderStack.Peek();

            shaderStack.Push(shader);

            if (shader == lastShader)
                return;

            FrameStatistics.Increment(StatisticsCounterType.ShaderBinds);

            flushCurrentBatch();
            graphicsBackend.SetShader(shader);
        }

        public void UnbindShader(Shader shader)
        {
            ThreadSafety.EnsureDrawThread();

            // todo: this should probably follow the behaviour of the other states.
            if (shaderStack.Peek() != shader)
                throw new InvalidOperationException("Attempting to unbind a shader while another one was bound.");

            shaderStack.Pop();

            // check if the stack is empty, and if so don't restore the previous shader.
            if (shaderStack.Count == 0)
                return;

            flushCurrentBatch();
            graphicsBackend.SetShader(shaderStack.Peek());
        }

        public void BindFrameBuffer(FrameBuffer frameBuffer)
        {
            if (frameBuffer == null) return;

            bool alreadyBound = frameBufferStack.Count > 0 && frameBufferStack.Peek() == frameBuffer;

            frameBufferStack.Push(frameBuffer);

            if (!alreadyBound)
            {
                flushCurrentBatch();

                graphicsBackend.SetFrameBuffer(frameBuffer);
                GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            }

            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        public void UnbindFrameBuffer(FrameBuffer frameBuffer)
        {
            if (frameBuffer == null) return;

            if (frameBufferStack.Peek() != frameBuffer)
                return;

            frameBufferStack.Pop();

            flushCurrentBatch();

            graphicsBackend.SetFrameBuffer(frameBuffer);
            GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        #endregion

        /// <summary>
        /// Frees resources unused after a while of frames.
        /// </summary>
        private void freeUnusedResources()
        {
            if (ResetId % resources_free_check_interval != 0)
                return;

            foreach (var buf in vertexBuffersInUse)
            {
                if (buf.InUse && ResetId - buf.LastUseResetId > resources_free_check_interval)
                    buf.Free();
            }

            vertexBuffersInUse.RemoveAll(b => !b.InUse);

            // commandsExecutionFencePool.FreeUnusedResources(resources_free_check_interval);
            // stagingBufferPool.FreeUnusedResources(resources_free_check_interval);
            // stagingTexturePool.FreeUnusedResources(resources_free_check_interval);
        }

        // /// <summary>
        // /// Releases resources marked as used to become available for subsequent consumption.
        // /// </summary>
        // private void releaseUsedResources()
        // {
        //     if (!(commandsExecutionFencePool.LatestSignaledUseID is ulong latestSignaledUseID))
        //         return;
        //
        //     commandsExecutionFencePool.ReleaseUsedResources(latestSignaledUseID);
        //     stagingBufferPool.ReleaseUsedResources(latestSignaledUseID);
        //     stagingTexturePool.ReleaseUsedResources(latestSignaledUseID);
        // }

        private void setMaskingInfo(MaskingInfo maskingInfo, bool isPushing)
        {
            if (MaskingInfo.Value == maskingInfo)
                return;

            flushCurrentBatch();

            GlobalPropertyManager.Set(GlobalProperty.MaskingRect, new Vector4(
                maskingInfo.MaskingRect.Left,
                maskingInfo.MaskingRect.Top,
                maskingInfo.MaskingRect.Right,
                maskingInfo.MaskingRect.Bottom));

            GlobalPropertyManager.Set(GlobalProperty.ToMaskingSpace, maskingInfo.ToMaskingSpace);

            GlobalPropertyManager.Set(GlobalProperty.CornerRadius, maskingInfo.CornerRadius);
            GlobalPropertyManager.Set(GlobalProperty.CornerExponent, maskingInfo.CornerExponent);

            GlobalPropertyManager.Set(GlobalProperty.BorderThickness, maskingInfo.BorderThickness / maskingInfo.BlendRange);

            if (maskingInfo.BorderThickness > 0)
            {
                GlobalPropertyManager.Set(GlobalProperty.BorderColour, new Vector4(
                    maskingInfo.BorderColour.Linear.R,
                    maskingInfo.BorderColour.Linear.G,
                    maskingInfo.BorderColour.Linear.B,
                    maskingInfo.BorderColour.Linear.A));
            }

            GlobalPropertyManager.Set(GlobalProperty.MaskingBlendRange, maskingInfo.BlendRange);
            GlobalPropertyManager.Set(GlobalProperty.AlphaExponent, maskingInfo.AlphaExponent);

            GlobalPropertyManager.Set(GlobalProperty.EdgeOffset, maskingInfo.EdgeOffset);

            GlobalPropertyManager.Set(GlobalProperty.DiscardInner, maskingInfo.Hollow);
            if (maskingInfo.Hollow)
                GlobalPropertyManager.Set(GlobalProperty.InnerCornerRadius, maskingInfo.HollowCornerRadius);

            if (isPushing)
            {
                // When drawing to a viewport that doesn't match the projection size (e.g. via framebuffers), the resultant image will be scaled
                Vector2 viewportScale = Vector2.Divide(Viewport.Value.Size, Ortho.Value.Size);

                Vector2 location = (maskingInfo.ScreenSpaceAABB.Location - ScissorOffset.Value) * viewportScale;
                var size = maskingInfo.ScreenSpaceAABB.Size * viewportScale;

                RectangleI actualRect = new RectangleI(
                    (int)Math.Floor(location.X),
                    (int)Math.Floor(location.Y),
                    (int)Math.Ceiling(size.X),
                    (int)Math.Ceiling(size.Y));

                Scissor.Push(RectangleI.Intersect(Scissor.Value, actualRect));
            }
            else
                Scissor.Pop();
        }

        private void setDepthInfo(DepthInfo depthInfo, bool isPushing)
        {
            flushCurrentBatch();

            if (DepthInfo.Value.Equals(depthInfo))
                return;

            graphicsBackend.DepthInfo = depthInfo;
        }

        private void setViewport(RectangleI viewport, bool isPushing)
        {
            if (isPushing)
            {
                Ortho.Push(viewport);

                if (viewport.Width < 0)
                {
                    viewport.X += viewport.Width;
                    viewport.Width = -viewport.Width;
                }

                if (viewport.Height < 0)
                {
                    viewport.Y += viewport.Height;
                    viewport.Height = -viewport.Height;
                }
            }
            else
                Ortho.Pop();

            if (Viewport.Value == viewport)
                return;

            graphicsBackend.SetViewport(viewport);
        }

        private void setOrtho(RectangleF rectangle, bool isPushing)
        {
            if (Ortho.Value == rectangle)
                return;

            var projectionMatrix = Matrix4.CreateOrthographicOffCenter(rectangle.Left, rectangle.Right, rectangle.Bottom, rectangle.Top, -1f, 1f);
            GlobalPropertyManager.Set(GlobalProperty.ProjMatrix, projectionMatrix);
        }

        private void setScissor(RectangleI scissor, bool isPushing)
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

            graphicsBackend.SetScissor(scissor);
        }

        private void setScissorState(bool scissorState, bool isPushing)
        {
            graphicsBackend.ScissorTest = scissorState;
        }

        private void flushCurrentBatch() => currentActiveBatch?.Draw();

        public void ScheduleDisposal<T>(Action<T> disposalAction, T target)
        {
            if (host != null)
                disposalQueue.ScheduleDisposal(disposalAction, target);
            else
                disposalAction.Invoke(target);
        }

        private void validateShaderLayout()
        {
            // var vertexShaderLayout = pipelineDescription.ShaderSet.VertexLayouts.Single();
            //
            // if (vertexShaderLayout.Elements.Length != boundVertexLayout.Elements.Length)
            //     throw new VertexLayoutMismatchException(boundShader.Name, vertexShaderLayout, boundVertexLayout, $"Length mismatch ({vertexShaderLayout.Elements.Length} != {boundVertexLayout.Elements.Length}).");
            //
            // for (int i = 0; i < vertexShaderLayout.Elements.Length; i++)
            // {
            //     var shaderElement = vertexShaderLayout.Elements[i];
            //     var bufferElement = boundVertexLayout.Elements[i];
            //
            //     if (shaderElement.Format != bufferElement.Format)
            //     {
            //         throw new VertexLayoutMismatchException(boundShader.Name, vertexShaderLayout, boundVertexLayout, $"Element {i - ShaderPart.BACKBUFFER_ATTRIBUTE_OFFSET} in vertex shader with format ({shaderElement.Format}) does not match corresponding element in vertex buffer layout ({bufferElement.Format}).");
            //     }
            // }
        }

        private class VertexLayoutMismatchException : Exception
        {
            public VertexLayoutMismatchException(string shaderName, IReadOnlyList<VertexLayoutElement> shaderLayout, IReadOnlyList<VertexLayoutElement> bufferLayout, string message)
                : base($"Vertex input layout mismatch between bound shader '{shaderName}' ({getDisplayString(shaderLayout)}) and bound vertex buffer ({getDisplayString(bufferLayout)}): {message}")
            {
            }

            private static string getDisplayString(IReadOnlyList<VertexLayoutElement> layout) => string.Join(", ", layout.Skip(ShaderPart.BACKBUFFER_ATTRIBUTE_OFFSET).Select(l => l.Type.Name));
        }
    }
}
