// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Development;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Buffers;
using osu.Framework.Graphics.Rendering.Textures;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osuTK;
using Veldrid;
using Veldrid.OpenGL;
using PrimitiveTopology = osu.Framework.Graphics.Rendering.PrimitiveTopology;
using Shader = osu.Framework.Graphics.Shaders.Shader;

namespace osu.Framework.Platform.Graphics
{
    /// <summary>
    /// An <see cref="IGraphicsBackend"/> implementation of <a href="https://github.com/mellinoe/veldrid">Veldrid</a>, a portable graphics library.
    /// </summary>
    public class VeldridGraphicsBackend : IGraphicsBackend
    {
        internal static readonly ResourceLayoutDescription UNIFORM_LAYOUT = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("m_Uniforms", ResourceKind.UniformBuffer, ShaderStages.Fragment | ShaderStages.Vertex));
        
        internal static readonly ResourceLayoutDescription TEXTURE_LAYOUT = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("m_Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("m_Sampler", ResourceKind.Sampler, ShaderStages.Fragment));

        internal const uint UNIFORM_RESOURCE_SLOT = 0;
        internal const uint TEXTURE_RESOURCE_SLOT = 1;

        public event Action OnSwap;

        public virtual GraphicsBackend Type
        {
            get
            {
                switch (RuntimeInfo.OS)
                {
                    case RuntimeInfo.Platform.Windows:
                        return GraphicsBackend.Direct3D11;

                    case RuntimeInfo.Platform.macOS:
                        return GraphicsBackend.Metal;

                    default:
                    case RuntimeInfo.Platform.Linux:
                        return GraphicsBackend.OpenGL;
                }
            }
        }

        /// <summary>
        /// The backing <see cref="GraphicsDevice"/> of this backend.
        /// </summary>
        protected GraphicsDevice Device { get; }

        public IGraphicsFactory Factory { get; }

        protected CommandList Commands { get; private set; }

        private readonly CommandList globalCommands;

        private readonly ResourceLayout uniformLayout;

        private GraphicsPipelineDescription pipelineDescription;

        private Framebuffer currentFrameBuffer;

        private VeldridTextureSet boundTextureSet;
        private ResourceSet boundUniformSet;

        public BlendingParameters BlendingParameters
        {
            set => pipelineDescription.BlendState.AttachmentStates = new[] { value.ToBlendAttachment() };
        }

        public DepthInfo DepthInfo
        {
            set
            {
                pipelineDescription.DepthStencilState.DepthTestEnabled = value.DepthTest;
                pipelineDescription.DepthStencilState.DepthWriteEnabled = value.WriteDepth;
                pipelineDescription.DepthStencilState.DepthComparison = value.Function;
            }
        }

        public bool ScissorTest
        {
            set => pipelineDescription.RasterizerState.ScissorTestEnabled = value;
        }

        public bool VerticalSync
        {
            get => Device.SyncToVerticalBlank;
            set => Device.SyncToVerticalBlank = value;
        }

        public VeldridGraphicsBackend(IWindow window, OpenGLOptions? glOptions = null, MetalOptions? metalOptions = null)
        {
            var options = new GraphicsDeviceOptions
            {
                HasMainSwapchain = true,
                SwapchainDepthFormat = null,
                // SwapchainSrgbFormat = true,
                SyncToVerticalBlank = true,
                PreferDepthRangeZeroToOne = true,
                PreferStandardClipSpaceYDirection = true,
                ResourceBindingModel = ResourceBindingModel.Improved,
                Debug = DebugUtils.IsDebugBuild,
            };

            Device = CreateDevice(options, glOptions, metalOptions, window);

            Factory = new VeldridGraphicsFactory(Device.ResourceFactory);

            globalCommands = Device.ResourceFactory.CreateCommandList();

            uniformLayout = Device.ResourceFactory.CreateResourceLayout(UNIFORM_LAYOUT);

            pipelineDescription.ResourceLayouts = new ResourceLayout[2];
            pipelineDescription.ResourceLayouts[UNIFORM_RESOURCE_SLOT] = uniformLayout;

            // var defaultTexture = Factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, TextureUsage.Sampled));
            //
            // using (BeginCommands())
            //     UpdateTexture(defaultTexture, 0, 0, 1, 1, 0, new ReadOnlySpan<Rgba32>(new[] { new Rgba32(0, 0, 0) }));
            //
            // defaultTextureSet = new TextureResourceSet(defaultTexture, Device.LinearSampler);
        }

        protected virtual GraphicsDevice CreateDevice(GraphicsDeviceOptions options, OpenGLOptions? glOptions, MetalOptions? metalOptions, IWindow window)
        {
            var swapchainDescription = new SwapchainDescription
            {
                Width = (uint)window.ClientSize.Width,
                Height = (uint)window.ClientSize.Height,
                ColorSrgb = options.SwapchainSrgbFormat,
                DepthFormat = options.SwapchainDepthFormat,
                SyncToVerticalBlank = options.SyncToVerticalBlank,
            };

            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    swapchainDescription.Source = SwapchainSource.CreateWin32(window.WindowHandle, IntPtr.Zero);
                    break;

                case RuntimeInfo.Platform.macOS:
                    if (Type == GraphicsBackend.Vulkan)
                    {
                        // Vulkan's validation layer is busted with Veldrid running on macOS.
                        // Waiting on https://github.com/mellinoe/veldrid/pull/419.
                        options.Debug = false;
                    }

                    if (!metalOptions.HasValue)
                        throw new ArgumentNullException(nameof(metalOptions), "Failed to create Metal-backed view. Missing Metal options.");

                    swapchainDescription.Source = SwapchainSource.CreateNSView(metalOptions.Value.CreateMetalView());
                    break;

                case RuntimeInfo.Platform.Linux:
                    swapchainDescription.Source = SwapchainSource.CreateXlib(window.DisplayHandle, window.WindowHandle);
                    break;
            }

            switch (Type)
            {
                case GraphicsBackend.OpenGL:
                    // SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);

                    if (!glOptions.HasValue)
                        throw new ArgumentNullException(nameof(glOptions), "Failed to create OpenGL device. Missing OpenGL options.");

                    IntPtr context = glOptions.Value.CreateContext();
                    // if (context == IntPtr.Zero)
                    //     throw new ArgumentNullException($"Failed to create an SDL2 GL context ({SDL.SDL_GetError()})");

                    return GraphicsDevice.CreateOpenGL(options, new OpenGLPlatformInfo(context,
                        s => glOptions.Value.GetProcAddress(s),
                        c => glOptions.Value.MakeCurrent(c),
                        () => glOptions.Value.GetCurrentContext(),
                        () => glOptions.Value.MakeCurrent(IntPtr.Zero),
                        c => glOptions.Value.DeleteContext(c),
                        () => glOptions.Value.SwapWindow(),
                        value => glOptions.Value.SetVerticalSync(value)), swapchainDescription.Width, swapchainDescription.Height);

                case GraphicsBackend.OpenGLES:
                    // SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_ES);
                    return GraphicsDevice.CreateOpenGLES(options, swapchainDescription);

                case GraphicsBackend.Direct3D11:
                    return GraphicsDevice.CreateD3D11(options, swapchainDescription);

                case GraphicsBackend.Vulkan:
                    return GraphicsDevice.CreateVulkan(options, swapchainDescription);

                case GraphicsBackend.Metal:
                    return GraphicsDevice.CreateMetal(options, swapchainDescription);
            }

            return null;
        }

        public void MakeCurrent()
        {
        }

        public void ClearCurrent()
        {
        }

        public void BeginDraw()
        {
            if (Commands == globalCommands)
                throw new InvalidOperationException("A draw session has already begun.");

            Commands = globalCommands;
            Commands.Begin();
        }

        // todo: add fence support to EndDraw() for proper CPU-GPU synchronisation when dealing with CPU buffer/texture pools.
        public void EndDraw()
        {
            if (Commands == null)
                throw new InvalidOperationException("No draw session began.");

            Commands.End();
            Device.SubmitCommands(Commands);

            Commands = null;
        }

        public void Resize(Vector2 size) => Device.ResizeMainWindow((uint)size.X, (uint)size.Y);

        public void SwapBuffers()
        {
            Device.SwapBuffers();
            OnSwap?.Invoke();
        }

        public void Clear(ClearInfo clearInfo)
        {
            Commands.ClearColorTarget(0, clearInfo.Colour.ToRgbaFloat());

            if (currentFrameBuffer.DepthTarget != null)
                Commands.ClearDepthStencil((float)clearInfo.Depth, (byte)clearInfo.Stencil);
        }

        public void SetViewport(RectangleI rectangle)
            => Commands.SetViewport(0, new Viewport(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, -1, 1));

        public void SetScissor(RectangleI rectangle)
            => Commands.SetScissorRect(0, (uint)rectangle.X, (uint)rectangle.Y, (uint)rectangle.Width, (uint)rectangle.Height);

        public void SetFrameBuffer(FrameBuffer framebuffer)
        {
            var framebufferResource = (Framebuffer)(framebuffer?.Resource ?? Device.SwapchainFramebuffer);
            Commands.SetFramebuffer(currentFrameBuffer = framebufferResource);
            pipelineDescription.Outputs = framebufferResource.OutputDescription;
        }

        public void SetVertexBuffer<TIndex>(IVertexBuffer buffer, IReadOnlyList<VertexLayoutElement> layout)
            where TIndex : unmanaged
        {
            Commands.SetVertexBuffer(0, (DeviceBuffer)buffer.VertexResource);

            // todo: maybe here, maybe somewhere else.
            // if (currentShader.VertexLayout.Elements == null || currentShader.VertexLayout.Elements.Length == 0)
            //     pipelineDescription.ShaderSet.VertexLayouts = new[] { layout };

            if (!Enum.TryParse<IndexFormat>(typeof(TIndex).Name, out var format))
                throw new InvalidOperationException($"'{typeof(TIndex).Name}' is an unsupported index format type. Only {string.Join(", ", Enum.GetNames(typeof(IndexFormat)))} are supported.");

            Commands.SetIndexBuffer((DeviceBuffer)buffer.IndexResource, format);
        }

        public void SetTexture(RendererTexture texture)
        {
            boundTextureSet = (VeldridTextureSet)texture.Resource;
            pipelineDescription.ResourceLayouts[TEXTURE_RESOURCE_SLOT] = boundTextureSet.Layout;
        }

        private readonly Dictionary<Shader, ResourceSet> shaderUniformSet = new Dictionary<Shader, ResourceSet>();

        public void SetShader(Shader shader)
        {
            pipelineDescription.ShaderSet.Shaders = shader.Shaders;

            if (!shaderUniformSet.TryGetValue(shader, out boundUniformSet))
            {
                // todo: add support for uniform buffer-like memory block in the shader itself.
                // var size = 
                // shaderUniformSet[shader] = boundUniformSet = 
            }
        }

        public unsafe void UpdateVertexBuffer<T>(IVertexBuffer buffer, int start, Memory<T> data)
            where T : unmanaged, IEquatable<T>, IVertex
            => Commands.UpdateBuffer((DeviceBuffer)buffer.VertexResource, (uint)(start * sizeof(T)), ref data.Span[0], (uint)(data.Length * sizeof(T)));

        public unsafe void UpdateTexture<TPixel>(RendererTexture texture, int x, int y, int width, int height, int level, Memory<TPixel> data)
            where TPixel : unmanaged
        {
            var textureSet = (VeldridTextureSet)texture.Resource;

            fixed (TPixel* ptr = data.Span)
                Device.UpdateTexture(textureSet.Texture, (IntPtr)ptr, (uint)(data.Length * sizeof(TPixel)), (uint)x, (uint)y, 0, (uint)width, (uint)height, 1, (uint)level, 0);
        }

        public void UpdateUniform<T>(IUniform<T> uniform) 
            where T : unmanaged, IEquatable<T>
        {
            switch (uniform)
            {
                case IUniform<Matrix3> matrix3:
                {
                    ref var value = ref matrix3.GetValueByRef();
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)(uniform.Location + 0), ref value.Row0);
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)(uniform.Location + 16), ref value.Row1);
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)(uniform.Location + 32), ref value.Row2);
                    break;
                }

                case IUniform<Matrix4> matrix4:
                {
                    ref var value = ref matrix4.GetValueByRef();
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)(uniform.Location + 0), ref value.Row0);
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)(uniform.Location + 16), ref value.Row1);
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)(uniform.Location + 32), ref value.Row2);
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)(uniform.Location + 48), ref value.Row3);
                    break;
                }

                default:
                    Commands.UpdateBuffer(uniform.Owner.UniformBuffer, (uint)uniform.Location, ref uniform.GetValueByRef());
                    break;
            }
        }

        public void UpdateUniforms(Shader shader)
        {
            // todo:
            // Commands.UpdateBuffer(shader.Uniforms);
        }

        private readonly Dictionary<GraphicsPipelineDescription, Pipeline> pipelineCache = new Dictionary<GraphicsPipelineDescription, Pipeline>();

        public void Draw(PrimitiveTopology topology, int start, int count)
        {
            pipelineDescription.PrimitiveTopology = topology.ToPrimitiveTopology();

            if (!pipelineCache.TryGetValue(pipelineDescription, out var pipeline))
                pipelineCache[pipelineDescription] = pipeline = Device.ResourceFactory.CreateGraphicsPipeline(ref pipelineDescription);

            Commands.SetPipeline(pipeline);
            Commands.SetGraphicsResourceSet(TEXTURE_RESOURCE_SLOT, boundTextureSet);
            Commands.SetGraphicsResourceSet(UNIFORM_RESOURCE_SLOT, boundUniformSet);
            Commands.DrawIndexed((uint)count, 1, (uint)start, 0, 0);
        }

        public void WaitUntilFinished() => Device.WaitForIdle();

        // public static ResourceSet CreateUniformResourceSet(DeviceBuffer buffer) => Factory.CreateResourceSet(new ResourceSetDescription(uniformLayout, buffer));
    }
}
