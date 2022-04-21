// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Development;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osuTK;
using Veldrid;
using static osu.Framework.Threading.ScheduledDelegate;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using VdShader = Veldrid.Shader;

namespace osu.Framework.Graphics.Veldrid
{
    public static partial class Vd
    {
        /// <summary>
        /// Maximum number of <see cref="DrawNode"/>s a <see cref="Drawable"/> can draw with.
        /// This is a carefully-chosen number to enable the update and draw threads to work concurrently without causing unnecessary load.
        /// </summary>
        public const int MAX_DRAW_NODES = 3;

        /// <summary>
        /// The amount of times <see cref="Reset"/> has been invoked.
        /// </summary>
        internal static ulong ResetId { get; private set; }

        /// <summary>
        /// The interval (in frames) before checking whether VBOs should be freed.
        /// VBOs may remain unused for at most double this length before they are recycled.
        /// </summary>
        private const int vbo_free_check_interval = 300;

        public static GraphicsDevice Device { get; private set; }

        public static ResourceFactory Factory => Device.ResourceFactory;

        public static CommandList Commands { get; private set; }

        private static GraphicsPipelineDescription pipelineDescription;

        public static ref readonly MaskingInfo CurrentMaskingInfo => ref currentMaskingInfo;
        private static MaskingInfo currentMaskingInfo;

        public static RectangleF Ortho { get; private set; }
        public static RectangleI Viewport { get; private set; }
        public static RectangleI Scissor { get; private set; }
        public static Vector2I ScissorOffset { get; private set; }
        public static Matrix4 ProjectionMatrix { get; set; }
        public static DepthInfo CurrentDepthInfo { get; private set; }

        public static float BackbufferDrawDepth { get; private set; }

        public static bool UsingBackbuffer => frame_buffer_stack.Peek() == Backbuffer;

        public static Framebuffer Backbuffer;

        public static int MaxTextureSize { get; private set; } = 4096;

        /// <summary>
        /// The maximum number of texture uploads to dequeue and upload per frame.
        /// Defaults to 32.
        /// </summary>
        public static int MaxTexturesUploadedPerFrame { get; set; } = 32;

        /// <summary>
        /// The maximum number of pixels to upload per frame.
        /// Defaults to 2 megapixels (8mb alloc).
        /// </summary>
        public static int MaxPixelsUploadedPerFrame { get; set; } = 1024 * 1024 * 2;

        private static readonly Scheduler reset_scheduler = new Scheduler(() => ThreadSafety.IsDrawThread, new StopwatchClock(true)); // force no thread set until we are actually on the draw thread.

        /// <summary>
        /// A queue from which a maximum of one operation is invoked per draw frame.
        /// </summary>
        private static readonly ConcurrentQueue<ScheduledDelegate> expensive_operation_queue = new ConcurrentQueue<ScheduledDelegate>();

        private static readonly ConcurrentQueue<VeldridTexture> texture_upload_queue = new ConcurrentQueue<VeldridTexture>();

        private static readonly List<IVertexBatch> batch_reset_list = new List<IVertexBatch>();

        private static readonly List<IVertexBuffer> vertex_buffers_in_use = new List<IVertexBuffer>();

        public static bool IsInitialized { get; private set; }

        private static WeakReference<GameHost> host;

        internal static void Initialise(GameHost host)
        {
            if (IsInitialized) return;

            Vd.host = new WeakReference<GameHost>(host);

            initialiseDevice(host);
            initialisePipeline();
            initialiseCommands();
            initialiseResources(ref pipelineDescription);

            Backbuffer = Device.SwapchainFramebuffer;

            IsInitialized = true;

            reset_scheduler.AddDelayed(checkPendingDisposals, 0, true);
        }

        private static readonly RendererDisposalQueue disposal_queue = new RendererDisposalQueue();

        internal static void ScheduleDisposal<T>(Action<T> disposalAction, T target)
        {
            if (host != null && host.TryGetTarget(out _))
                disposal_queue.ScheduleDisposal(disposalAction, target);
            else
                disposalAction.Invoke(target);
        }

        private static void checkPendingDisposals()
        {
            disposal_queue.CheckPendingDisposals();
        }

        private static readonly GlobalStatistic<int> stat_expensive_operations_queued = GlobalStatistics.Get<int>("Veldrid", "Expensive operation queue length");
        private static readonly GlobalStatistic<int> stat_texture_uploads_queued = GlobalStatistics.Get<int>("Veldrid", "Texture upload queue length");
        private static readonly GlobalStatistic<int> stat_texture_uploads_dequeued = GlobalStatistics.Get<int>("Veldrid", "Texture uploads dequeued");
        private static readonly GlobalStatistic<int> stat_texture_uploads_performed = GlobalStatistics.Get<int>("Veldrid", "Texture uploads performed");

        private static Vector2 currentSize;

        public static void Reset(Vector2 size)
        {
            ResetId++;

            Trace.Assert(shader_stack.Count == 0);

            reset_scheduler.Update();

            stat_expensive_operations_queued.Value = expensive_operation_queue.Count;

            while (expensive_operation_queue.TryDequeue(out ScheduledDelegate operation))
            {
                if (operation.State == RunState.Waiting)
                {
                    operation.RunTask();
                    break;
                }
            }

            lastActiveBatch = null;
            lastBlendingParameters = new BlendingParameters();

            foreach (var b in batch_reset_list)
                b.ResetCounters();

            batch_reset_list.Clear();

            viewport_stack.Clear();
            ortho_stack.Clear();
            masking_stack.Clear();
            scissor_rect_stack.Clear();
            frame_buffer_stack.Clear();
            depth_stack.Clear();
            scissor_state_stack.Clear();
            scissor_offset_stack.Clear();

            if (size != currentSize)
            {
                // todo: look for better window resize handling
                Device.MainSwapchain.Resize((uint)size.X, (uint)size.Y);

                currentSize = size;
            }

            if (Backbuffer.IsDisposed)
                Backbuffer = Device.SwapchainFramebuffer;

            BindFrameBuffer(Backbuffer);

            Scissor = RectangleI.Empty;
            ScissorOffset = Vector2I.Zero;
            Viewport = RectangleI.Empty;
            Ortho = RectangleF.Empty;

            PushScissorState(true);
            PushViewport(new RectangleI(0, 0, (int)size.X, (int)size.Y));
            PushScissor(new RectangleI(0, 0, (int)size.X, (int)size.Y));
            PushScissorOffset(Vector2I.Zero);
            PushMaskingInfo(new MaskingInfo
            {
                ScreenSpaceAABB = new RectangleI(0, 0, (int)size.X, (int)size.Y),
                MaskingRect = new RectangleF(0, 0, size.X, size.Y),
                ToMaskingSpace = Matrix3.Identity,
                BlendRange = 1,
                AlphaExponent = 1,
                CornerExponent = 2.5f,
            }, true);

            PushDepthInfo(DepthInfo.Default);
            Clear(ClearInfo.Default);

            freeUnusedVertexBuffers();

            stat_texture_uploads_queued.Value = texture_upload_queue.Count;
            stat_texture_uploads_dequeued.Value = 0;
            stat_texture_uploads_performed.Value = 0;

            // increase the number of items processed with the queue length to ensure it doesn't get out of hand.
            int targetUploads = Math.Clamp(texture_upload_queue.Count / 2, 1, MaxTexturesUploadedPerFrame);
            int uploads = 0;
            int uploadedPixels = 0;

            // continue attempting to upload textures until enough uploads have been performed.
            while (texture_upload_queue.TryDequeue(out VeldridTexture texture))
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

            // boundTextureSet = defaultTextureSet;
            boundVertexBuffer = null;
        }

        /// <summary>
        /// Enqueues a texture to be uploaded in the next frame.
        /// </summary>
        /// <param name="veldridTexture">The texture to be uploaded.</param>
        public static void EnqueueTextureUpload(VeldridTexture veldridTexture)
        {
            if (veldridTexture.IsQueuedForUpload)
                return;

            if (host != null)
            {
                veldridTexture.IsQueuedForUpload = true;
                texture_upload_queue.Enqueue(veldridTexture);
            }
        }

        /// <summary>
        /// Schedules an expensive operation to a queue from which a maximum of one operation is performed per frame.
        /// </summary>
        /// <param name="operation">The operation to schedule.</param>
        public static void ScheduleExpensiveOperation(ScheduledDelegate operation)
        {
            if (host != null)
                expensive_operation_queue.Enqueue(operation);
        }

        private static IVertexBatch lastActiveBatch;

        /// <summary>
        /// Sets the last vertex batch used for drawing.
        /// <para>
        /// This is done so that various methods that change renderer state can force-draw the batch
        /// before continuing with the state change.
        /// </para>
        /// </summary>
        /// <param name="batch">The batch.</param>
        internal static void SetActiveBatch(IVertexBatch batch)
        {
            if (lastActiveBatch == batch)
                return;

            batch_reset_list.Add(batch);

            FlushCurrentBatch();

            lastActiveBatch = batch;
        }

        /// <summary>
        /// Notifies that a <see cref="IVertexBuffer"/> has begun being used.
        /// </summary>
        /// <param name="buffer">The <see cref="IVertexBuffer"/> in use.</param>
        internal static void RegisterVertexBufferUse(IVertexBuffer buffer) => vertex_buffers_in_use.Add(buffer);

        private static void freeUnusedVertexBuffers()
        {
            if (ResetId % vbo_free_check_interval != 0)
                return;

            foreach (var buf in vertex_buffers_in_use)
            {
                if (buf.InUse && ResetId - buf.LastUseResetId > vbo_free_check_interval)
                    buf.Free();
            }

            vertex_buffers_in_use.RemoveAll(b => !b.InUse);
        }

        private static readonly Stack<Vector2I> scissor_offset_stack = new Stack<Vector2I>();

        /// <summary>
        /// Applies an offset to the scissor rectangle.
        /// </summary>
        /// <param name="offset">The offset.</param>
        public static void PushScissorOffset(Vector2I offset)
        {
            FlushCurrentBatch();

            scissor_offset_stack.Push(offset);
            if (ScissorOffset == offset)
                return;

            ScissorOffset = offset;
        }

        /// <summary>
        /// Applies the last scissor rectangle offset.
        /// </summary>
        public static void PopScissorOffset()
        {
            Trace.Assert(scissor_offset_stack.Count > 1);

            FlushCurrentBatch();

            scissor_offset_stack.Pop();
            Vector2I offset = scissor_offset_stack.Peek();

            if (ScissorOffset == offset)
                return;

            ScissorOffset = offset;
        }

        private static readonly Stack<RectangleF> ortho_stack = new Stack<RectangleF>();

        /// <summary>
        /// Applies a new orthographic projection rectangle.
        /// </summary>
        /// <param name="rectangle">The orthographic projection rectangle.</param>
        public static void PushOrtho(RectangleF rectangle)
        {
            FlushCurrentBatch();

            ortho_stack.Push(rectangle);
            if (Ortho == rectangle)
                return;

            Ortho = rectangle;
            setProjectionMatrix(rectangle);
        }

        /// <summary>
        /// Applies the last orthographic projection rectangle.
        /// </summary>
        public static void PopOrtho()
        {
            Trace.Assert(ortho_stack.Count > 1);

            FlushCurrentBatch();

            ortho_stack.Pop();
            RectangleF rectangle = ortho_stack.Peek();

            if (Ortho == rectangle)
                return;

            Ortho = rectangle;
            setProjectionMatrix(rectangle);
        }

        private static void setProjectionMatrix(RectangleF rectangle)
        {
            // Inverse the near/far values to not affect with depth values during multiplication.
            // todo: replace this with a custom implementation or otherwise.
            ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(rectangle.Left, rectangle.Right, rectangle.Bottom, rectangle.Top, 1f, -1f);

            GlobalPropertyManager.Set(GlobalProperty.ProjMatrix, ProjectionMatrix);
        }

        private static readonly Stack<MaskingInfo> masking_stack = new Stack<MaskingInfo>();
        private static readonly Stack<RectangleI> scissor_rect_stack = new Stack<RectangleI>();
        private static readonly Stack<Framebuffer> frame_buffer_stack = new Stack<Framebuffer>();
        private static readonly Stack<DepthInfo> depth_stack = new Stack<DepthInfo>();

        private static void setMaskingInfo(MaskingInfo maskingInfo, bool isPushing, bool overwritePreviousScissor)
        {
            FlushCurrentBatch();

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
                GlobalPropertyManager.Set(GlobalProperty.BorderColour, new Matrix4(
                    // TopLeft
                    maskingInfo.BorderColour.TopLeft.Linear.R,
                    maskingInfo.BorderColour.TopLeft.Linear.G,
                    maskingInfo.BorderColour.TopLeft.Linear.B,
                    maskingInfo.BorderColour.TopLeft.Linear.A,
                    // BottomLeft
                    maskingInfo.BorderColour.BottomLeft.Linear.R,
                    maskingInfo.BorderColour.BottomLeft.Linear.G,
                    maskingInfo.BorderColour.BottomLeft.Linear.B,
                    maskingInfo.BorderColour.BottomLeft.Linear.A,
                    // TopRight
                    maskingInfo.BorderColour.TopRight.Linear.R,
                    maskingInfo.BorderColour.TopRight.Linear.G,
                    maskingInfo.BorderColour.TopRight.Linear.B,
                    maskingInfo.BorderColour.TopRight.Linear.A,
                    // BottomRight
                    maskingInfo.BorderColour.BottomRight.Linear.R,
                    maskingInfo.BorderColour.BottomRight.Linear.G,
                    maskingInfo.BorderColour.BottomRight.Linear.B,
                    maskingInfo.BorderColour.BottomRight.Linear.A));
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
                Vector2 viewportScale = Vector2.Divide(Viewport.Size, Ortho.Size);

                Vector2 location = (maskingInfo.ScreenSpaceAABB.Location - ScissorOffset) * viewportScale;
                Vector2 size = maskingInfo.ScreenSpaceAABB.Size * viewportScale;

                RectangleI actualRect = new RectangleI(
                    (int)Math.Floor(location.X),
                    (int)Math.Floor(location.Y),
                    (int)Math.Ceiling(size.X),
                    (int)Math.Ceiling(size.Y));

                PushScissor(overwritePreviousScissor ? actualRect : RectangleI.Intersect(scissor_rect_stack.Peek(), actualRect));
            }
            else
                PopScissor();
        }

        internal static void FlushCurrentBatch()
        {
            lastActiveBatch?.Draw();
        }

        public static bool IsMaskingActive => masking_stack.Count > 1;

        /// <summary>
        /// Applies a new scissor rectangle.
        /// </summary>
        /// <param name="maskingInfo">The masking info.</param>
        /// <param name="overwritePreviousScissor">Whether or not to shrink an existing scissor rectangle.</param>
        public static void PushMaskingInfo(in MaskingInfo maskingInfo, bool overwritePreviousScissor = false)
        {
            masking_stack.Push(maskingInfo);
            if (CurrentMaskingInfo == maskingInfo)
                return;

            currentMaskingInfo = maskingInfo;
            setMaskingInfo(CurrentMaskingInfo, true, overwritePreviousScissor);
        }

        /// <summary>
        /// Applies the last scissor rectangle.
        /// </summary>
        public static void PopMaskingInfo()
        {
            Trace.Assert(masking_stack.Count > 1);

            masking_stack.Pop();
            MaskingInfo maskingInfo = masking_stack.Peek();

            if (CurrentMaskingInfo == maskingInfo)
                return;

            currentMaskingInfo = maskingInfo;
            setMaskingInfo(CurrentMaskingInfo, false, true);
        }

        /// <summary>
        /// Sets the current draw depth.
        /// The draw depth is written to every vertex added to <see cref="VertexBuffer{T}"/>s.
        /// </summary>
        /// <param name="drawDepth">The draw depth.</param>
        internal static void SetDrawDepth(float drawDepth) => BackbufferDrawDepth = drawDepth;

        /// <summary>
        /// Sets the current backbuffer.
        /// </summary>
        /// <remarks>
        /// This is used for the ability to perform screenshots by submitting a full render pass to a specific framebuffer.
        /// </remarks>
        /// <param name="framebuffer">The framebuffer to be considered as the backbuffer.</param>
        internal static IDisposable SetBackbuffer(Framebuffer framebuffer)
        {
            var currentBackbuffer = Backbuffer;
            Backbuffer = framebuffer;

            return new ValueInvokeOnDisposal<Framebuffer>(currentBackbuffer, c => Backbuffer = c);
        }
    }
}
