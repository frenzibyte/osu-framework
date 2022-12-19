// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Shaders;
using osuTK;

namespace osu.Framework.Graphics.OpenGL.Shaders
{
    internal class GLGlobalUniformManager : IGlobalUniformManager
    {
        private readonly HashSet<GLShader> allShaders = new HashSet<GLShader>();
        private readonly IGLUniformMapping[] globalProperties;

        public GLGlobalUniformManager()
        {
            var values = Enum.GetValues(typeof(GlobalProperty)).OfType<GlobalProperty>().ToArray();

            globalProperties = new IGLUniformMapping[values.Length];

            globalProperties[(int)GlobalProperty.ProjMatrix] = new GLUniformMapping<Matrix4>("g_ProjMatrix");
            globalProperties[(int)GlobalProperty.IsMasking] = new GLUniformMapping<bool>("g_IsMasking");
            globalProperties[(int)GlobalProperty.MaskingRect] = new GLUniformMapping<Vector4>("g_MaskingRect");
            globalProperties[(int)GlobalProperty.ToMaskingSpace] = new GLUniformMapping<Matrix3>("g_ToMaskingSpace");
            globalProperties[(int)GlobalProperty.CornerRadius] = new GLUniformMapping<float>("g_CornerRadius");
            globalProperties[(int)GlobalProperty.CornerExponent] = new GLUniformMapping<float>("g_CornerExponent");
            globalProperties[(int)GlobalProperty.BorderThickness] = new GLUniformMapping<float>("g_BorderThickness");
            globalProperties[(int)GlobalProperty.BorderColour] = new GLUniformMapping<Matrix4>("g_BorderColour");
            globalProperties[(int)GlobalProperty.MaskingBlendRange] = new GLUniformMapping<float>("g_MaskingBlendRange");
            globalProperties[(int)GlobalProperty.AlphaExponent] = new GLUniformMapping<float>("g_AlphaExponent");
            globalProperties[(int)GlobalProperty.EdgeOffset] = new GLUniformMapping<Vector2>("g_EdgeOffset");
            globalProperties[(int)GlobalProperty.DiscardInner] = new GLUniformMapping<bool>("g_DiscardInner");
            globalProperties[(int)GlobalProperty.InnerCornerRadius] = new GLUniformMapping<float>("g_InnerCornerRadius");
            globalProperties[(int)GlobalProperty.GammaCorrection] = new GLUniformMapping<bool>("g_GammaCorrection");
            globalProperties[(int)GlobalProperty.WrapModeS] = new GLUniformMapping<int>("g_WrapModeS");
            globalProperties[(int)GlobalProperty.WrapModeT] = new GLUniformMapping<int>("g_WrapModeT");

            // Backbuffer internals
            globalProperties[(int)GlobalProperty.BackbufferDraw] = new GLUniformMapping<bool>("g_BackbufferDraw");
        }

        void IGlobalUniformManager.Set<T>(GlobalProperty property, T value) => ((GLUniformMapping<T>)globalProperties[(int)property]).UpdateValue(ref value);

        public void Register(GLShader shader)
        {
            if (!allShaders.Add(shader)) return;

            // transfer all existing global properties across.
            foreach (var global in globalProperties)
            {
                if (!shader.Uniforms.TryGetValue(global.Name, out IUniform uniform))
                    continue;

                global.LinkShaderUniform(uniform);
            }
        }

        public void Unregister(GLShader shader)
        {
            if (!allShaders.Remove(shader)) return;

            foreach (var global in globalProperties)
            {
                if (!shader.Uniforms.TryGetValue(global.Name, out IUniform uniform))
                    continue;

                global.UnlinkShaderUniform(uniform);
            }
        }

        public bool Exists(string name) => globalProperties.Any(m => m.Name == name);
    }
}
