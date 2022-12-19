// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Veldrid.Shaders;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridGlobalUniformManager : IGlobalUniformManager
    {
        private readonly Dictionary<string, IUniform> uniforms;
        private readonly IUniform[] uniformValues;

        internal static readonly ResourceLayoutDescription GLOBAL_UNIFORMS_LAYOUT = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("m_GlobalUniforms", ResourceKind.UniformBuffer, ShaderStages.Fragment | ShaderStages.Vertex));

        /// <summary>
        /// The global uniforms buffer.
        /// </summary>
        public DeviceBuffer Buffer { get; }

        /// <summary>
        /// The layout of the <see cref="ResourceSet"/> containing the uniform buffer.
        /// </summary>
        public ResourceLayout ResourceLayout { get; }

        /// <summary>
        /// The resource set containing the global uniform buffer.
        /// </summary>
        public ResourceSet ResourceSet { get; }

        public VeldridGlobalUniformManager(VeldridRenderer renderer)
        {
            var group = new VeldridUniformGroup();

            group.AddUniform("g_ProjMatrix", "mat4");
            group.AddUniform("g_IsMasking", "bool");
            group.AddUniform("g_MaskingRect", "vec4");
            group.AddUniform("g_ToMaskingSpace", "mat3");
            group.AddUniform("g_CornerRadius", "float");
            group.AddUniform("g_CornerExponent", "float");
            group.AddUniform("g_BorderThickness", "float");
            group.AddUniform("g_BorderColour", "mat4");
            group.AddUniform("g_MaskingBlendRange", "float");
            group.AddUniform("g_AlphaExponent", "float");
            group.AddUniform("g_EdgeOffset", "vec2");
            group.AddUniform("g_DiscardInner", "bool");
            group.AddUniform("g_InnerCornerRadius", "float");
            group.AddUniform("g_GammaCorrection", "bool");
            group.AddUniform("g_WrapModeS", "int");
            group.AddUniform("g_WrapModeT", "int");
            group.AddUniform("g_BackbufferDraw", "bool");

            Buffer = group.CreateBuffer(renderer, null, out uniforms);
            uniformValues = uniforms.Values.ToArray();

            ResourceLayout = renderer.Factory.CreateResourceLayout(GLOBAL_UNIFORMS_LAYOUT);
            ResourceSet = renderer.Factory.CreateResourceSet(new ResourceSetDescription(ResourceLayout, Buffer));
        }

        void IGlobalUniformManager.Set<T>(GlobalProperty property, T value) => ((Uniform<T>)uniforms[$"g_{property}"]).Value = value;

        public void RefreshUniforms()
        {
            foreach (var uniform in uniformValues)
                uniform.Update();
        }
    }
}
