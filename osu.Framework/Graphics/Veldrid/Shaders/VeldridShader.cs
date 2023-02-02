// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;
using Veldrid;
using Veldrid.SPIRV;
using static osu.Framework.Threading.ScheduledDelegate;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal class VeldridShader : IShader
    {
        private readonly string name;
        private readonly VeldridShaderPart[] parts;
        private readonly IUniformBuffer<GlobalUniformData> globalUniformBuffer;
        private readonly VeldridRenderer renderer;

        public Shader[]? Shaders;

        private readonly ScheduledDelegate shaderInitialiseDelegate;

        public bool IsLoaded => Shaders != null;

        public bool IsBound { get; private set; }

        IReadOnlyDictionary<string, IUniform> IShader.Uniforms => throw new NotSupportedException();
        public int LayoutCount => uniformLayouts.Count + textureLayouts.Count;

        private readonly Dictionary<string, VeldridUniformLayout> uniformLayouts = new Dictionary<string, VeldridUniformLayout>();
        private readonly List<VeldridUniformLayout> textureLayouts = new List<VeldridUniformLayout>();

        public VeldridShader(VeldridRenderer renderer, string name, VeldridShaderPart[] parts, IUniformBuffer<GlobalUniformData> globalUniformBuffer)
        {
            this.name = name;
            this.parts = parts;
            this.globalUniformBuffer = globalUniformBuffer;
            this.renderer = renderer;

            renderer.ScheduleExpensiveOperation(shaderInitialiseDelegate = new ScheduledDelegate(initialise));
        }

        internal void EnsureShaderInitialised()
        {
            if (isDisposed)
                throw new ObjectDisposedException(ToString(), "Can not compile a disposed shader.");

            if (shaderInitialiseDelegate.State == RunState.Waiting)
                shaderInitialiseDelegate.RunTask();
        }

        public void Bind()
        {
            if (IsBound)
                return;

            EnsureShaderInitialised();

            renderer.BindShader(this);

            IsBound = true;
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            renderer.UnbindShader(this);
            IsBound = false;
        }

        public Uniform<T> GetUniform<T>(string name) where T : unmanaged, IEquatable<T> => throw new NotSupportedException();

        public void AssignUniformBlock(string blockName, IUniformBuffer buffer)
        {
            if (buffer is not IVeldridUniformBuffer veldridBuffer)
                throw new InvalidOperationException();

            renderer.AssignUniformBuffer(blockName, veldridBuffer);
        }

        public VeldridUniformLayout? GetTextureLayout(int textureUnit) => textureUnit >= textureLayouts.Count ? null : textureLayouts[textureUnit];

        public VeldridUniformLayout? GetUniformBufferLayout(string name) => uniformLayouts.GetValueOrDefault(name);

        private void initialise()
        {
            Debug.Assert(parts.Length == 2);

            VeldridShaderPart vertex = parts.Single(p => p.Type == ShaderPartType.Vertex);
            VeldridShaderPart fragment = parts.Single(p => p.Type == ShaderPartType.Fragment);

            try
            {
                ShaderDescription vertexShaderDescription = new ShaderDescription(
                    ShaderStages.Vertex,
                    Array.Empty<byte>(),
                    "main");

                ShaderDescription fragmentShaderDescription = new ShaderDescription(
                    ShaderStages.Fragment,
                    Array.Empty<byte>(),
                    "main");

                // GLSL cross compile is always performed for reflection, even though the cross-compiled shaders aren't used under Vulkan.
                VertexFragmentCompilationResult crossCompileResult = SpirvCompilation.CompileVertexFragment(
                    Encoding.UTF8.GetBytes(vertex.Data),
                    Encoding.UTF8.GetBytes(fragment.Data),
                    CrossCompileTarget.GLSL,
                    new CrossCompileOptions
                    {
                        FixClipSpaceZ = !renderer.Device.IsDepthRangeZeroToOne,
                        InvertVertexOutputY = renderer.Device.IsClipSpaceYInverted,
                    });

                if (renderer.SurfaceType == GraphicsSurfaceType.Vulkan)
                {
                    vertexShaderDescription.ShaderBytes = SpirvCompilation.CompileGlslToSpirv(vertex.Data, null, ShaderStages.Vertex, GlslCompileOptions.Default).SpirvBytes;
                    fragmentShaderDescription.ShaderBytes = SpirvCompilation.CompileGlslToSpirv(fragment.Data, null, ShaderStages.Fragment, GlslCompileOptions.Default).SpirvBytes;
                }
                else
                {
                    VertexFragmentCompilationResult platformCrossCompileResult = crossCompileResult;

                    // If we don't have an OpenGL surface, we need to cross-compile once more for the correct platform.
                    if (renderer.SurfaceType != GraphicsSurfaceType.OpenGL)
                    {
                        CrossCompileTarget target = renderer.SurfaceType switch
                        {
                            GraphicsSurfaceType.Metal => CrossCompileTarget.MSL,
                            GraphicsSurfaceType.Direct3D11 => CrossCompileTarget.HLSL,
                            _ => throw new InvalidOperationException($"Unsupported surface type: {renderer.SurfaceType}.")
                        };

                        platformCrossCompileResult = SpirvCompilation.CompileVertexFragment(
                            Encoding.UTF8.GetBytes(vertex.Data),
                            Encoding.UTF8.GetBytes(fragment.Data),
                            target,
                            new CrossCompileOptions
                            {
                                FixClipSpaceZ = !renderer.Device.IsDepthRangeZeroToOne,
                                InvertVertexOutputY = renderer.Device.IsClipSpaceYInverted,
                            });
                    }

                    vertexShaderDescription.ShaderBytes = Encoding.UTF8.GetBytes(platformCrossCompileResult.VertexShader);
                    fragmentShaderDescription.ShaderBytes = Encoding.UTF8.GetBytes(platformCrossCompileResult.FragmentShader);
                }

                Shader vertexShader = renderer.Factory.CreateShader(vertexShaderDescription);
                Shader fragmentShader = renderer.Factory.CreateShader(fragmentShaderDescription);

                Shaders = new[] { vertexShader, fragmentShader };

                for (int set = 0; set < crossCompileResult.Reflection.ResourceLayouts.Length; set++)
                {
                    ResourceLayoutDescription layout = crossCompileResult.Reflection.ResourceLayouts[set];

                    if (layout.Elements.Length == 0)
                        continue;

                    if (layout.Elements.Any(e => e.Kind == ResourceKind.TextureReadOnly || e.Kind == ResourceKind.TextureReadWrite))
                    {
                        // Todo: We should enforce that a texture set contains both a texture and a sampler.
                        var textureElement = layout.Elements.First(e => e.Kind == ResourceKind.TextureReadOnly || e.Kind == ResourceKind.TextureReadWrite);
                        var samplerElement = layout.Elements.First(e => e.Kind == ResourceKind.Sampler);

                        textureLayouts.Add(new VeldridUniformLayout(
                            set,
                            renderer.Factory.CreateResourceLayout(
                                new ResourceLayoutDescription(
                                    new ResourceLayoutElementDescription(
                                        textureElement.Name,
                                        ResourceKind.TextureReadOnly,
                                        ShaderStages.Fragment),
                                    new ResourceLayoutElementDescription(
                                        samplerElement.Name,
                                        ResourceKind.Sampler,
                                        ShaderStages.Fragment)))));
                    }
                    else if (layout.Elements[0].Kind == ResourceKind.UniformBuffer)
                    {
                        uniformLayouts[layout.Elements[0].Name] = new VeldridUniformLayout(
                            set,
                            renderer.Factory.CreateResourceLayout(
                                new ResourceLayoutDescription(
                                    new ResourceLayoutElementDescription(
                                        layout.Elements[0].Name,
                                        ResourceKind.UniformBuffer,
                                        ShaderStages.Fragment | ShaderStages.Vertex))));
                    }
                }

                AssignUniformBlock("g_GlobalUniforms", globalUniformBuffer);
            }
            catch (SpirvCompilationException e)
            {
                Logger.Error(e, $"Failed to initialise shader \"{name}\"");
            }
        }

        private bool isDisposed;

        ~VeldridShader()
        {
            renderer.ScheduleDisposal(s => s.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            if (Shaders != null)
            {
                for (int i = 0; i < Shaders.Length; i++)
                    Shaders[i].Dispose();

                Shaders = null;
            }
        }
    }
}