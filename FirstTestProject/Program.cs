// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Configuration;
using osu.Framework.Extensions.ImageExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;
using Veldrid;
using Veldrid.SPIRV;
using Image = SixLabors.ImageSharp.Image;
using PrimitiveTopology = Veldrid.PrimitiveTopology;

// ReSharper disable RedundantExplicitParamsArrayCreation

namespace FirstTestProject
{
    public static class Program
    {
        private static SDL2DesktopWindow window;

        private static GraphicsDevice device;
        private static ResourceFactory factory;

        private static GraphicsPipelineDescription pipelineDescription;

        private static Texture texture;
        private static TextureView textureView;
        private static Sampler sampler;

        private static DeviceBuffer uniformBuffer;

        private static ResourceLayout resourceLayout;
        private static ResourceSet resourceSet;

        private static DeviceBuffer vertexBuffer;
        private static DeviceBuffer indexBuffer;

        private static Shader[] shaders;

        private static Pipeline pipeline;

        [STAThread]
        public static void Main()
        {
            window = new SDL2DesktopWindow();
            window.SetupWindow(new FrameworkConfigManager(new NativeStorage("~/.local/share/osu-framework-veldrid")));
            window.Create();

            window.Visible = true;
            window.Title = "osu!framework (running under Veldrid)";

            device = Renderer.Device;
            factory = Renderer.Device.ResourceFactory;

            setupTextures();
            setupUniforms();
            setupResources();
            setupVertices();
            setupShaders();

            pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.Less),
                RasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, false, false),
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = new[] { resourceLayout },
                // ResourceLayouts = Array.Empty<ResourceLayout>(),
                ShaderSet = new ShaderSetDescription(new[] { VertexUtils<TexturedVertex2D>.Layout }, shaders),
                Outputs = device.SwapchainFramebuffer.OutputDescription,
            };

            pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            var commandList = factory.CreateCommandList();

            // ReSharper disable AccessToDisposedClosure
            window.Update += () =>
            {
                commandList.Begin();

                commandList.SetFramebuffer(device.SwapchainFramebuffer);

                commandList.ClearColorTarget(0, RgbaFloat.Black);

                commandList.SetViewport(0, new Viewport(0, 0, window.Size.Width, window.Size.Height, 0, 1));

                commandList.UpdateBuffer(uniformBuffer, 0, new GlobalUniformStructure
                {
                    ProjMatrix = Matrix4.Identity,
                    ToMaskingSpace = Matrix3.Identity,
                });

                commandList.SetVertexBuffer(0, vertexBuffer);
                commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);

                commandList.SetPipeline(pipeline);
                commandList.SetGraphicsResourceSet(0, resourceSet);

                commandList.DrawIndexed(6, 1, 0, 0, 0);

                commandList.End();

                device.SubmitCommands(commandList);
                device.SwapBuffers();
            };

            window.Run();

            commandList.Dispose();

            texture.Dispose();
            textureView.Dispose();
            sampler.Dispose();
            resourceLayout.Dispose();
            resourceSet.Dispose();
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            uniformBuffer.Dispose();
            pipeline.Dispose();
        }

        private static void setupTextures()
        {
            using (var image = Image.Load(File.ReadAllBytes("/Users/salman/Desktop/osu-framework-veldrid/osu.Framework.Tests/Resources/Textures/sample-texture.png")))
            using (var pixels = image.CreateReadOnlyPixelSpan())
            {
                texture = factory.CreateTexture(TextureDescription.Texture2D((uint)image.Width, (uint)image.Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, TextureUsage.Sampled, TextureSampleCount.Count1));
                device.UpdateTexture(texture, pixels.Span.ToArray(), 0, 0, 0, (uint)image.Width, (uint)image.Height, 1, 0, 0);
            }

            textureView = factory.CreateTextureView(texture);

            sampler = factory.CreateSampler(new SamplerDescription
            {
                AddressModeU = SamplerAddressMode.Clamp,
                AddressModeV = SamplerAddressMode.Clamp,
                AddressModeW = SamplerAddressMode.Clamp,
                Filter = SamplerFilter.MinLinear_MagLinear_MipLinear,
                LodBias = 0,
                MinimumLod = 0,
                MaximumLod = 3,
                MaximumAnisotropy = 0,
            });
        }

        private static unsafe void setupUniforms()
        {
            uniformBuffer = factory.CreateBuffer(new BufferDescription((uint)(Math.Ceiling((double)sizeof(GlobalUniformStructure) / 16) * 16), BufferUsage.UniformBuffer));
        }

        private static void setupResources()
        {
            resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(new[]
            {
                new ResourceLayoutElementDescription("m_Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("m_Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("m_Uniforms", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            }));

            resourceSet = factory.CreateResourceSet(new ResourceSetDescription(resourceLayout, texture, sampler, uniformBuffer));
        }

        private static void setupVertices()
        {
            vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)VertexUtils<TexturedVertex2D>.STRIDE * 4, BufferUsage.VertexBuffer));

            var quad = new Quad(0.5f - ((float)texture.Width / window.Size.Width) / 2, 0.5f - ((float)texture.Height / window.Size.Height) / 2, (float)texture.Width / window.Size.Width, (float)texture.Height / window.Size.Height);
            quad = new Quad(quad.TopLeft * 2 - Vector2.One, quad.TopRight * 2 - Vector2.One, quad.BottomLeft * 2 - Vector2.One, quad.BottomRight * 2 - Vector2.One);

            device.UpdateBuffer(vertexBuffer, 0, new[]
            {
                new TexturedVertex2D
                {
                    Position = quad.BottomLeft,
                    TexturePosition = new Vector2(0, 1),
                    TextureRect = new Vector4(0, 0, 1366, 768),
                    BlendRange = Vector2.Zero,
                    Colour = Color4.Red,
                },
                new TexturedVertex2D
                {
                    Position = quad.BottomRight,
                    TexturePosition = new Vector2(1, 1),
                    TextureRect = new Vector4(0, 0, 1366, 768),
                    BlendRange = Vector2.Zero,
                    Colour = Color4.Green,
                },
                new TexturedVertex2D
                {
                    Position = quad.TopRight,
                    TexturePosition = new Vector2(1, 0),
                    TextureRect = new Vector4(0, 0, 1366, 768),
                    BlendRange = Vector2.Zero,
                    Colour = Color4.Yellow,
                },
                new TexturedVertex2D
                {
                    Position = quad.TopLeft,
                    TexturePosition = new Vector2(0, 0),
                    TextureRect = new Vector4(0, 0, 1366, 768),
                    BlendRange = Vector2.Zero,
                    Colour = Color4.Blue,
                },
            });

            const int indices_per_quad = 6;

            ushort[] indices = new ushort[indices_per_quad];

            for (ushort i = 0, j = 0; j < indices_per_quad; i += 4, j += indices_per_quad)
            {
                indices[j] = i;
                indices[j + 1] = (ushort)(i + 1);
                indices[j + 2] = (ushort)(i + 3);
                indices[j + 3] = (ushort)(i + 2);
                indices[j + 4] = (ushort)(i + 3);
                indices[j + 5] = (ushort)(i + 1);
            }

            indexBuffer = factory.CreateBuffer(new BufferDescription((uint)(sizeof(ushort) * indices.Length), BufferUsage.IndexBuffer));

            device.UpdateBuffer(indexBuffer, 0, indices);
        }

        private static void setupShaders()
        {
            var vertexShader = new ShaderDescription(ShaderStages.Vertex, File.ReadAllBytes("/Users/salman/Desktop/osu-framework-veldrid/osu.Framework/Resources/Shaders/sh_Texture2D.vs"), "main", true);
            var fragmentShader = new ShaderDescription(ShaderStages.Fragment, File.ReadAllBytes("/Users/salman/Desktop/osu-framework-veldrid/osu.Framework/Resources/Shaders/sh_Texture.fs"), "main", true);

            shaders = factory.CreateFromSpirv(vertexShader, fragmentShader, new CrossCompileOptions(true, true));
        }
    }
}

