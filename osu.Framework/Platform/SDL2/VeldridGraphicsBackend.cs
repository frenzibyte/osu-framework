// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using osu.Framework.Development;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Renderer;
using osu.Framework.Graphics.Renderer.Buffers;
using osu.Framework.Graphics.Renderer.Textures;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osuTK;
using SDL2;
using Veldrid;
using Veldrid.OpenGL;
using Veldrid.OpenGLBinding;
using static osu.Framework.Threading.ScheduledDelegate;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using Shader = osu.Framework.Graphics.Shaders.Shader;
using Vector2 = System.Numerics.Vector2;

namespace osu.Framework.Platform.SDL2
{
    /// <summary>
    /// Implementation of that uses Veldrid bindings.
    /// </summary>
    public class VeldridGraphicsBackend : IGraphicsBackend
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

        // ReSharper disable once RedundantExplicitParamsArrayCreation
        private static readonly ResourceLayoutDescription shader_resource_layout = new ResourceLayoutDescription(new[]
        {
            new ResourceLayoutElementDescription("m_Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("m_Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("m_Uniforms", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
        });

        private SDL2DesktopWindow sdlWindow;

        public static GraphicsDevice Device { get; private set; }

        public static ResourceFactory Factory => Device.ResourceFactory;

        public static VeldridResourceSet ResourceSet { get; private set; }

        public static ref readonly MaskingInfo CurrentMaskingInfo => ref currentMaskingInfo;
        private static MaskingInfo currentMaskingInfo;

        public static CommandList Commands { get; private set; }
        public static Pipeline Pipeline { get; private set; }

        private static GraphicsPipelineDescription pipelineDescription;

        public static RectangleF Ortho { get; private set; }
        public static RectangleI Viewport { get; private set; }
        public static RectangleI Scissor { get; private set; }
        public static Vector2I ScissorOffset { get; private set; }
        public static Matrix4 ProjectionMatrix { get; set; }
        public static DepthInfo CurrentDepthInfo { get; private set; }

        public static float BackbufferDrawDepth { get; private set; }

        public static bool UsingBackbuffer => frame_buffer_stack.Peek() == DefaultFrameBuffer;

        public static Framebuffer DefaultFrameBuffer;

        public static bool IsEmbedded { get; internal set; }

        public static int MaxTextureSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a GL context, like headless CI.
        public static int MaxRenderBufferSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a GL context, like headless CI.

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

        public bool VerticalSync
        {
            get => Device.SyncToVerticalBlank;
            set => Device.SyncToVerticalBlank = value;
        }

        private static readonly Scheduler reset_scheduler = new Scheduler(() => ThreadSafety.IsDrawThread, new StopwatchClock(true)); // force no thread set until we are actually on the draw thread.

        /// <summary>
        /// A queue from which a maximum of one operation is invoked per draw frame.
        /// </summary>
        private static readonly ConcurrentQueue<ScheduledDelegate> expensive_operation_queue = new ConcurrentQueue<ScheduledDelegate>();

        private static readonly ConcurrentQueue<RendererTexture> texture_upload_queue = new ConcurrentQueue<RendererTexture>();

        private static readonly List<IVertexBatch> batch_reset_list = new List<IVertexBatch>();

        private static readonly List<IVertexBuffer> vertex_buffers_in_use = new List<IVertexBuffer>();

        public static bool IsInitialized { get; private set; }

        private static WeakReference<GameHost> host;

        public static GameHost Host
        {
            get => host?.TryGetTarget(out var result) == true ? result : null;
            set => host = new WeakReference<GameHost>(value);
        }

        public void Initialise(IWindow window)
        {
            if (IsInitialized) return;

            if (!(window is SDL2DesktopWindow))
                throw new ArgumentException("Unsupported window backend.", nameof(window));

            sdlWindow = (SDL2DesktopWindow)window;

            var options = new GraphicsDeviceOptions
            {
                HasMainSwapchain = true,
                SwapchainDepthFormat = null,
                // SwapchainSrgbFormat = true,
                SyncToVerticalBlank = true,
                PreferDepthRangeZeroToOne = true,
                PreferStandardClipSpaceYDirection = true,
                ResourceBindingModel = ResourceBindingModel.Improved,
                Debug = true,
            };

            Device = createDevice(options, sdlWindow, window.ClientSize);

            Logger.Log($@"{Device.BackendType} Initialised
                          {Device.BackendType} ComputeShader: {Device.Features.ComputeShader}
                          {Device.BackendType} GeometryShader: {Device.Features.GeometryShader}
                          {Device.BackendType} TessellationShaders: {Device.Features.TessellationShaders}
                          {Device.BackendType} MultipleViewports: {Device.Features.MultipleViewports}
                          {Device.BackendType} SamplerLodBias: {Device.Features.SamplerLodBias}
                          {Device.BackendType} DrawBaseVertex: {Device.Features.DrawBaseVertex}
                          {Device.BackendType} DrawBaseInstance: {Device.Features.DrawBaseInstance}
                          {Device.BackendType} DrawIndirect: {Device.Features.DrawIndirect}
                          {Device.BackendType} DrawIndirectBaseInstance: {Device.Features.DrawIndirectBaseInstance}
                          {Device.BackendType} FillModeWireframe: {Device.Features.FillModeWireframe}
                          {Device.BackendType} SamplerAnisotropy: {Device.Features.SamplerAnisotropy}
                          {Device.BackendType} DepthClipDisable: {Device.Features.DepthClipDisable}
                          {Device.BackendType} Texture1D: {Device.Features.Texture1D}
                          {Device.BackendType} IndependentBlend: {Device.Features.IndependentBlend}
                          {Device.BackendType} StructuredBuffer: {Device.Features.StructuredBuffer}
                          {Device.BackendType} SubsetTextureView: {Device.Features.SubsetTextureView}
                          {Device.BackendType} CommandListDebugMarkers: {Device.Features.CommandListDebugMarkers}
                          {Device.BackendType} BufferRangeBinding: {Device.Features.BufferRangeBinding}");

            DefaultFrameBuffer = Device.SwapchainFramebuffer;

            ResourceSet = new VeldridResourceSet(shader_resource_layout);

            pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.Less),
                RasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, false, false),
                ResourceLayouts = new[] { ResourceSet.ResourceLayout },
                ShaderSet = new ShaderSetDescription(Array.Empty<VertexLayoutDescription>(), Array.Empty<Veldrid.Shader>()),
                Outputs = DefaultFrameBuffer.OutputDescription,
            };

            Commands = Factory.CreateCommandList();

            IsInitialized = true;

            reset_scheduler.AddDelayed(checkPendingDisposals, 0, true);
        }

        private GraphicsDevice createDevice(GraphicsDeviceOptions options, SDL2DesktopWindow sdlWindow, Size initialSize)
        {
            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    return GraphicsDevice.CreateD3D11(options, sdlWindow.WindowHandle, (uint)initialSize.Width, (uint)initialSize.Height);

                case RuntimeInfo.Platform.macOS:
                    return GraphicsDevice.CreateMetal(options, sdlWindow.WindowHandle);

                case RuntimeInfo.Platform.Linux:
                    SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_COMPATIBILITY);

                    IntPtr context = SDL.SDL_GL_CreateContext(sdlWindow.SDLWindowHandle);
                    if (context == IntPtr.Zero)
                        throw new InvalidOperationException($"Failed to create an SDL2 GL context ({SDL.SDL_GetError()})");

                    return GraphicsDevice.CreateOpenGL(options, new OpenGLPlatformInfo(context,
                        s => SDL.SDL_GL_GetProcAddress(s),
                        c => SDL.SDL_GL_MakeCurrent(sdlWindow.SDLWindowHandle, c),
                        () => SDL.SDL_GL_GetCurrentContext(),
                        () => SDL.SDL_GL_MakeCurrent(sdlWindow.SDLWindowHandle, IntPtr.Zero),
                        c => SDL.SDL_GL_DeleteContext(c),
                        () => SDL.SDL_GL_SwapWindow(sdlWindow.SDLWindowHandle),
                        value => SDL.SDL_GL_SetSwapInterval(value ? 1 : 0)), (uint)initialSize.Width, (uint)initialSize.Height);
            }

            return null;
        }

        public Size GetDrawableSize()
        {
            int width = 0;
            int height = 0;

            switch (Device.BackendType)
            {
                case GraphicsBackend.OpenGL:
                case GraphicsBackend.OpenGLES:
                    SDL.SDL_GL_GetDrawableSize(sdlWindow.SDLWindowHandle, out width, out height);
                    break;

                case GraphicsBackend.Vulkan:
                    SDL.SDL_Vulkan_GetDrawableSize(sdlWindow.SDLWindowHandle, out width, out height);
                    break;

                case GraphicsBackend.Metal:
                    SDL.SDL_Metal_GetDrawableSize(sdlWindow.SDLWindowHandle, out width, out height);
                    break;
            }

            return new Size(width, height);
        }

        public void SwapBuffers() => Device.SwapBuffers();

        private static readonly RendererDisposalQueue disposal_queue = new RendererDisposalQueue();

        internal static void ScheduleDisposal(Action disposalAction)
        {
            if (host != null && host.TryGetTarget(out _))
                disposal_queue.ScheduleDisposal(disposalAction);
            else
                disposalAction.Invoke();
        }

        private static void checkPendingDisposals()
        {
            disposal_queue.CheckPendingDisposals();
        }

        private static readonly GlobalStatistic<int> stat_expensive_operations_queued = GlobalStatistics.Get<int>("Veldrid", "Expensive operation queue length");
        private static readonly GlobalStatistic<int> stat_texture_uploads_queued = GlobalStatistics.Get<int>("Veldrid", "Texture upload queue length");
        private static readonly GlobalStatistic<int> stat_texture_uploads_dequeued = GlobalStatistics.Get<int>("Veldrid", "Texture uploads dequeued");
        private static readonly GlobalStatistic<int> stat_texture_uploads_performed = GlobalStatistics.Get<int>("Veldrid", "Texture uploads performed");

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

            BindFrameBuffer(DefaultFrameBuffer);

            Scissor = RectangleI.Empty;
            ScissorOffset = Vector2I.Zero;
            Viewport = RectangleI.Empty;
            Ortho = RectangleF.Empty;

            Device.MainSwapchain.Resize((uint)size.X, (uint)size.Y);

            PushScissorState(false);
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

            Commands.ClearColorTarget(0, RgbaFloat.Black);

            freeUnusedVertexBuffers();

            stat_texture_uploads_queued.Value = texture_upload_queue.Count;
            stat_texture_uploads_dequeued.Value = 0;
            stat_texture_uploads_performed.Value = 0;

            // increase the number of items processed with the queue length to ensure it doesn't get out of hand.
            int targetUploads = Math.Clamp(texture_upload_queue.Count / 2, 1, MaxTexturesUploadedPerFrame);
            int uploads = 0;
            int uploadedPixels = 0;

            // continue attempting to upload textures until enough uploads have been performed.
            while (texture_upload_queue.TryDequeue(out RendererTexture texture))
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

            last_bound_textures.AsSpan().Clear();

            lastBoundBuffer = null;
        }

        private static ClearInfo currentClearInfo;

        public static void Clear(ClearInfo clearInfo)
        {
            PushDepthInfo(new DepthInfo(writeDepth: true));
            PushScissorState(false);

            if (clearInfo.Colour != currentClearInfo.Colour)
                Commands.ClearColorTarget(0, clearInfo.Colour.ToRgbaFloat());

            if (clearInfo.Depth != currentClearInfo.Depth || clearInfo.Stencil != currentClearInfo.Stencil)
                Commands.ClearDepthStencil((float)clearInfo.Depth, (byte)clearInfo.Stencil);

            currentClearInfo = clearInfo;

            PopScissorState();
            PopDepthInfo();
        }

        private static readonly Stack<bool> scissor_state_stack = new Stack<bool>();

        private static bool currentScissorState;

        public static void PushScissorState(bool enabled)
        {
            scissor_state_stack.Push(enabled);
            setScissorState(enabled);
        }

        public static void PopScissorState()
        {
            Trace.Assert(scissor_state_stack.Count > 1);

            scissor_state_stack.Pop();

            setScissorState(scissor_state_stack.Peek());
        }

        private static void setScissorState(bool enabled)
        {
            if (enabled == currentScissorState)
                return;

            currentScissorState = enabled;

            pipelineDescription.RasterizerState.ScissorTestEnabled = enabled;
        }

        /// <summary>
        /// Enqueues a texture to be uploaded in the next frame.
        /// </summary>
        /// <param name="texture">The texture to be uploaded.</param>
        public static void EnqueueTextureUpload(RendererTexture texture)
        {
            if (texture.IsQueuedForUpload)
                return;

            if (host != null)
            {
                texture.IsQueuedForUpload = true;
                texture_upload_queue.Enqueue(texture);
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

        private static DeviceBuffer lastBoundBuffer;

        public static bool BindVertexBuffer(DeviceBuffer buffer, VertexLayoutDescription layout)
        {
            if (buffer == lastBoundBuffer)
                return false;

            Commands.SetVertexBuffer(0, buffer);

            pipelineDescription.ShaderSet.VertexLayouts = new[] { layout };

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);

            lastBoundBuffer = buffer;
            return true;
        }

        public static void BindIndexBuffer(DeviceBuffer buffer, IndexFormat format) => Commands.SetIndexBuffer(buffer, format);

        private static IVertexBatch lastActiveBatch;

        /// <summary>
        /// Sets the last vertex batch used for drawing.
        /// <para>
        /// This is done so that various methods that change GL state can force-draw the batch
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

        private static readonly (Texture texture, Sampler sampler, bool isAtlas)[] last_bound_textures = new (Texture, Sampler, bool)[16];

        internal static int GetTextureUnitId(TextureUnit unit) => (int)unit - (int)TextureUnit.Texture0;
        internal static bool AtlasTextureIsBound(TextureUnit unit) => last_bound_textures[GetTextureUnitId(unit)].isAtlas;

        /// <summary>
        /// Binds a texture to draw with.
        /// </summary>
        /// <param name="texture">The texture to bind.</param>
        /// <param name="unit">The texture unit to bind it to.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>true if the provided texture was not already bound (causing a binding change).</returns>
        public static bool BindTexture(RendererTexture texture, TextureUnit unit = TextureUnit.Texture0, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            bool didBind = BindTexture(texture?.Texture, texture?.Sampler, unit, wrapModeS, wrapModeT);
            last_bound_textures[GetTextureUnitId(unit)].isAtlas = texture is RendererTextureAtlas;

            return didBind;
        }

        internal static WrapMode CurrentWrapModeS;
        internal static WrapMode CurrentWrapModeT;

        /// <summary>
        /// Binds a texture to draw with.
        /// </summary>
        /// <param name="texture">The texture to bind.</param>
        /// <param name="sampler">The sampler to bind with the texture.</param>
        /// <param name="unit">The texture unit to bind it to.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>true if the provided texture was not already bound (causing a binding change).</returns>
        internal static bool BindTexture(Texture texture, Sampler sampler, TextureUnit unit = TextureUnit.Texture0, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            int index = GetTextureUnitId(unit);

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

            if (last_bound_textures[index].texture == texture && last_bound_textures[index].sampler == sampler)
                return false;

            FlushCurrentBatch();

            ResourceSet.SetResource(ResourceKind.TextureReadOnly, texture);
            ResourceSet.SetResource(ResourceKind.Sampler, sampler);

            last_bound_textures[index] = (texture, sampler, false);

            FrameStatistics.Increment(StatisticsCounterType.TextureBinds);
            return true;
        }

        private static BlendingParameters lastBlendingParameters;

        /// <summary>
        /// Sets the blending function to draw with.
        /// </summary>
        /// <param name="blendingParameters">The info we should use to update the active state.</param>
        public static void SetBlend(BlendingParameters blendingParameters)
        {
            if (lastBlendingParameters == blendingParameters)
                return;

            FlushCurrentBatch();

            pipelineDescription.BlendState = new BlendStateDescription(default, blendingParameters.ToBlendAttachment());
            lastBlendingParameters = blendingParameters;
        }

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
            ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(rectangle.X, rectangle.X + rectangle.Width, rectangle.Y, rectangle.Y + rectangle.Height, -1f, 1f);
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
                osuTK.Vector2 viewportScale = osuTK.Vector2.Divide(Viewport.Size, Ortho.Size);

                osuTK.Vector2 location = (maskingInfo.ScreenSpaceAABB.Location - ScissorOffset) * viewportScale;
                osuTK.Vector2 size = maskingInfo.ScreenSpaceAABB.Size * viewportScale;

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
        /// Applies a new depth information.
        /// </summary>
        /// <param name="depthInfo">The depth information.</param>
        public static void PushDepthInfo(DepthInfo depthInfo)
        {
            depth_stack.Push(depthInfo);

            if (CurrentDepthInfo.Equals(depthInfo))
                return;

            CurrentDepthInfo = depthInfo;
            setDepthInfo(CurrentDepthInfo);
        }

        /// <summary>
        /// Applies the last depth information.
        /// </summary>
        public static void PopDepthInfo()
        {
            Trace.Assert(depth_stack.Count > 1);

            depth_stack.Pop();
            DepthInfo depthInfo = depth_stack.Peek();

            if (CurrentDepthInfo.Equals(depthInfo))
                return;

            CurrentDepthInfo = depthInfo;
            setDepthInfo(CurrentDepthInfo);
        }

        private static void setDepthInfo(DepthInfo depthInfo)
        {
            FlushCurrentBatch();

            pipelineDescription.DepthStencilState.DepthTestEnabled = depthInfo.DepthTest;
            pipelineDescription.DepthStencilState.DepthWriteEnabled = depthInfo.WriteDepth;
            pipelineDescription.DepthStencilState.DepthComparison = depthInfo.Function;
        }

        /// <summary>
        /// Sets the current draw depth.
        /// The draw depth is written to every vertex added to <see cref="VertexBuffer{T}"/>s.
        /// </summary>
        /// <param name="drawDepth">The draw depth.</param>
        internal static void SetDrawDepth(float drawDepth) => BackbufferDrawDepth = drawDepth;

        private static GraphicsPipelineDescription lastPipelineDescription;

        public static void DrawVertices(PrimitiveTopology topology, int verticesStart, int verticesCount)
        {
            pipelineDescription.PrimitiveTopology = topology;

            if (!pipelineDescription.Equals(lastPipelineDescription))
            {
                Pipeline?.Dispose();
                Pipeline = Factory.CreateGraphicsPipeline(pipelineDescription);
            }

            Commands.SetPipeline(Pipeline);
            Commands.SetGraphicsResourceSet(0, ResourceSet.GetResourceSet());

            Commands.DrawIndexed((uint)verticesCount, 1, (uint)verticesStart, 0, 0);

            lastPipelineDescription = pipelineDescription;
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

            ScheduleDisposal(frameBuffer.Dispose);
        }

        private static Shader currentShader;

        private static readonly Stack<Shader> shader_stack = new Stack<Shader>();

        public static void BindShader(Shader shader)
        {
            ThreadSafety.EnsureDrawThread();

            shader_stack.Push(shader);

            if (shader == currentShader)
                return;

            FrameStatistics.Increment(StatisticsCounterType.ShaderBinds);

            setShader(shader);
        }

        public static void UnbindShader(Shader shader)
        {
            ThreadSafety.EnsureDrawThread();

            if (shader != currentShader)
                throw new InvalidOperationException("Attempting to unbind shader while not current.");

            shader_stack.Pop();

            // check if the stack is empty, and if so don't restore the previous shader.
            if (shader_stack.Count == 0)
                return;

            setShader(shader_stack.Peek());
        }

        private static void setShader(Shader shader)
        {
            FlushCurrentBatch();

            pipelineDescription.ShaderSet.Shaders = shader?.Shaders ?? Array.Empty<Veldrid.Shader>();
            ResourceSet.SetResource(ResourceKind.UniformBuffer, shader?.UniformBuffer);

            currentShader = shader;
        }

        internal static void UpdateUniform<T>(IUniformWithValue<T> uniform)
            where T : struct, IEquatable<T>
        {
            if (uniform.Owner == currentShader)
                FlushCurrentBatch();

            switch (uniform)
            {
                case IUniformWithValue<bool> _:
                case IUniformWithValue<int> _:
                case IUniformWithValue<float> _:
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)uniform.Location, uniform.GetValue());
                    break;

                case IUniformWithValue<Vector2> _:
                case IUniformWithValue<Vector3> _:
                case IUniformWithValue<Vector4> _:
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)uniform.Location, ref uniform.GetValueByRef());
                    break;

                case IUniformWithValue<Matrix3> matrix3:
                {
                    ref var value = ref matrix3.GetValueByRef();
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)uniform.Location + 0, ref value.Row0);
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)uniform.Location + 16, ref value.Row1);
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)uniform.Location + 32, ref value.Row2);
                    break;
                }

                case IUniformWithValue<Matrix4> matrix4:
                {
                    ref var value = ref matrix4.GetValueByRef();
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)uniform.Location + 0, ref value.Row0);
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)uniform.Location + 16, ref value.Row1);
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)uniform.Location + 32, ref value.Row2);
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)uniform.Location + 48, ref value.Row3);
                    break;
                }
            }
        }

        void IGraphicsBackend.MakeCurrent()
        {
        }

        void IGraphicsBackend.ClearCurrent()
        {
        }
    }
}
