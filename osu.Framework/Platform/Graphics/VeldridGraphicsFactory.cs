// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Text;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering.Textures;
using osu.Framework.Graphics.Rendering.Vertices;
using Veldrid;
using Veldrid.SPIRV;
using PixelFormat = osu.Framework.Graphics.Rendering.Textures.PixelFormat;

namespace osu.Framework.Platform.Graphics
{
    public class VeldridGraphicsFactory : IGraphicsFactory
    {
        private readonly ResourceFactory factory;

        public VeldridGraphicsFactory(ResourceFactory factory)
        {
            this.factory = factory;
        }

        public IDisposable CreateTexture(int width, int height, PixelFormat format, int maximumLevels)
        {
            int mipLevels = Math.Min(maximumLevels, calculateMipmapLevels(width, height));
            var description = TextureDescription.Texture2D((uint)width, (uint)height, (uint)mipLevels, 1, format.ToPixelFormat(), TextureUsage.Sampled);
            return factory.CreateTexture(description);
        }

        public IDisposable CreateVertexBuffer(int length) => factory.CreateBuffer(new BufferDescription((uint)length, BufferUsage.VertexBuffer));

        public IDisposable CreateIndexBuffer(int length) => factory.CreateBuffer(new BufferDescription((uint)length, BufferUsage.IndexBuffer));

        public IDisposable[] CreateVertexFragmentShaders(byte[] vertexBytes, byte[] fragmentBytes, out VertexLayoutElement[] elements)
        {
            if (factory.BackendType == GraphicsBackend.Vulkan)
            {
                vertexBytes = ensureSpirv(ShaderStages.Vertex, vertexBytes);
                fragmentBytes = ensureSpirv(ShaderStages.Fragment, fragmentBytes);
                elements = null;
            }
            else
            {
                // todo: maybe fix fixClipSpaceZ
                var result = SpirvCompilation.CompileVertexFragment(vertexBytes, fragmentBytes, getCompilationTarget(factory.BackendType), new CrossCompileOptions(false, false));

                switch (factory.BackendType)
                {
                    case GraphicsBackend.Direct3D11:
                    case GraphicsBackend.OpenGL:
                    case GraphicsBackend.OpenGLES:
                        vertexBytes = Encoding.ASCII.GetBytes(result.VertexShader);
                        fragmentBytes = Encoding.ASCII.GetBytes(result.FragmentShader);
                        break;

                    case GraphicsBackend.Metal:
                        vertexBytes = Encoding.UTF8.GetBytes(result.VertexShader);
                        fragmentBytes = Encoding.UTF8.GetBytes(result.FragmentShader);
                        break;
                }

                elements = result.Reflection.VertexElements.Select(e => new VertexLayoutElement(e.Format) { Name = e.Name }).ToArray();
            }

            return new IDisposable[]
            {
                factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexBytes, factory.BackendType == GraphicsBackend.Metal ? "main0" : "main")),
                factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentBytes, factory.BackendType == GraphicsBackend.Metal ? "main0" : "main"))
            };
        }

        public IDisposable CreateFrameBuffer(RendererTexture target, PixelFormat[] renderFormats, PixelFormat? depthFormat = null)
        {
            var description = new FramebufferDescription
            {
                ColorTargets = new FramebufferAttachmentDescription[1 + (renderFormats?.Length ?? 0)]
            };

            description.ColorTargets[0] = new FramebufferAttachmentDescription(((VeldridTextureSet)target.Resource).Texture, 0);

            if (renderFormats != null)
            {
                for (int i = 0; i < renderFormats.Length; i++)
                {
                    var targetDescription = TextureDescription.Texture2D((uint)target.Width, (uint)target.Height, 1, 1, renderFormats[i].ToPixelFormat(), TextureUsage.RenderTarget);
                    description.ColorTargets[1 + i] = new FramebufferAttachmentDescription(factory.CreateTexture(targetDescription), 0);
                }
            }

            if (depthFormat != null)
            {
                var targetDescription = TextureDescription.Texture2D((uint)target.Width, (uint)target.Height, 1, 1, depthFormat.Value.ToPixelFormat(), TextureUsage.DepthStencil);
                description.DepthTarget = new FramebufferAttachmentDescription(factory.CreateTexture(targetDescription), 0);
            }

            return factory.CreateFramebuffer(description);
        }

        private static int calculateMipmapLevels(int width, int height) => 1 + (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

        private static CrossCompileTarget getCompilationTarget(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Direct3D11:
                    return CrossCompileTarget.HLSL;

                case GraphicsBackend.OpenGL:
                    return CrossCompileTarget.GLSL;

                case GraphicsBackend.Metal:
                    return CrossCompileTarget.MSL;

                case GraphicsBackend.OpenGLES:
                    return CrossCompileTarget.ESSL;

                default:
                    throw new ArgumentOutOfRangeException(nameof(backend), backend, null);
            }
        }

        private static byte[] ensureSpirv(ShaderStages type, byte[] bytes)
        {
            if (bytes[0] == 3 && bytes[1] == 2 && bytes[2] == 35 && bytes[3] == 7)
                return bytes;

            return SpirvCompilation.CompileGlslToSpirv(Encoding.UTF8.GetString(bytes), null, type, new GlslCompileOptions(DebugUtils.IsDebugBuild)).SpirvBytes;
        }

        // private static readonly Dictionary<int, ResourceLayout> texture_layouts = new Dictionary<int, ResourceLayout>();
        //
        // /// <summary>
        // /// Retrieves a <see cref="ResourceLayout"/> for a texture resource set.
        // /// </summary>
        // /// <param name="textureCount">The number of textures in the resource layout.</param>
        // /// <returns></returns>
        // public static ResourceLayout GetTextureResourceLayout(int textureCount)
        // {
        //     if (texture_layouts.TryGetValue(textureCount, out var layout))
        //         return layout;
        //
        //     var description = new ResourceLayoutDescription(new ResourceLayoutElementDescription[textureCount + 1]);
        //     var textureElement = TEXTURE_LAYOUT.Elements.Single(e => e.Kind == ResourceKind.TextureReadOnly);
        //
        //     for (int i = 0; i < textureCount; i++)
        //         description.Elements[i] = new ResourceLayoutElementDescription($"{textureElement.Name}{i}", textureElement.Kind, textureElement.Stages);
        //
        //     description.Elements[^1] = TEXTURE_LAYOUT.Elements.Single(e => e.Kind == ResourceKind.Sampler);
        //     return texture_layouts[textureCount] = Factory.CreateResourceLayout(description);
        // }
    }
}
