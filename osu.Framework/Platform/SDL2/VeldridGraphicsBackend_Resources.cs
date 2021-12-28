// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using osu.Framework.Graphics.Renderer.Textures;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Statistics;
using osuTK;
using Veldrid;
using Shader = osu.Framework.Graphics.Shaders.Shader;

namespace osu.Framework.Platform.SDL2
{
    public partial class VeldridGraphicsBackend
    {
        internal const uint UNIFORM_RESOURCE_SLOT = 0;
        internal const uint TEXTURE_RESOURCE_SLOT = 1;

        internal static readonly ResourceLayoutDescription UNIFORM_LAYOUT = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("m_Uniforms", ResourceKind.UniformBuffer, ShaderStages.Fragment | ShaderStages.Vertex));

        internal static readonly ResourceLayoutDescription TEXTURE_LAYOUT = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("m_Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("m_Sampler", ResourceKind.Sampler, ShaderStages.Fragment));

        private static ResourceLayout uniformLayout;

        private static TextureResourceSet defaultTextureSet;
        private static TextureResourceSet boundTextureSet;

        public static bool AtlasTextureIsBound { get; private set; }

        private static void initialiseResources(ref GraphicsPipelineDescription description)
        {
            uniformLayout = Factory.CreateResourceLayout(UNIFORM_LAYOUT);

            description.ResourceLayouts = new ResourceLayout[2];
            description.ResourceLayouts[UNIFORM_RESOURCE_SLOT] = uniformLayout;

            var defaultTexture = Factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, TextureUsage.Sampled));
            var defaultSampler = Device.LinearSampler;
            defaultTextureSet = new TextureResourceSet(defaultTexture, defaultSampler);
        }

        #region Textures

        /// <summary>
        /// Binds a texture to draw with.
        /// </summary>
        /// <param name="texture">The texture to bind.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>true if the provided texture was not already bound (causing a binding change).</returns>
        public static bool BindTexture(RendererTexture texture, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            bool didBind = BindTexture(texture?.TextureResourceSet, wrapModeS, wrapModeT);
            AtlasTextureIsBound = texture is RendererTextureAtlas;

            return didBind;
        }

        internal static WrapMode CurrentWrapModeS;
        internal static WrapMode CurrentWrapModeT;

        /// <summary>
        /// Binds a texture to draw with.
        /// </summary>
        /// <param name="textureSet">The texture resource set to bind.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>true if the provided texture was not already bound (causing a binding change).</returns>
        internal static bool BindTexture(TextureResourceSet textureSet, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
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

            if (boundTextureSet == textureSet)
                return false;

            FlushCurrentBatch();

            boundTextureSet = textureSet;
            pipelineDescription.ResourceLayouts[TEXTURE_RESOURCE_SLOT] = textureSet.Layout;

            AtlasTextureIsBound = false;

            FrameStatistics.Increment(StatisticsCounterType.TextureBinds);
            return true;
        }

        /// <summary>
        /// Resets bound textures and binds a default texture to draw with.
        /// </summary>
        internal static void ResetTexture() => BindTexture(defaultTextureSet);

        private static readonly Dictionary<int, ResourceLayout> texture_layouts = new Dictionary<int, ResourceLayout>();

        /// <summary>
        /// Retrieves a <see cref="ResourceLayout"/> for a texture resource set.
        /// </summary>
        /// <param name="textureCount">The number of textures in the resource layout.</param>
        /// <returns></returns>
        public static ResourceLayout GetTextureResourceLayout(int textureCount)
        {
            if (texture_layouts.TryGetValue(textureCount, out var layout))
                return layout;

            var description = new ResourceLayoutDescription(new ResourceLayoutElementDescription[textureCount + 1]);
            var textureElement = TEXTURE_LAYOUT.Elements.Single(e => e.Kind == ResourceKind.TextureReadOnly);

            for (int i = 0; i < textureCount; i++)
                description.Elements[i] = new ResourceLayoutElementDescription($"{textureElement.Name}{i}", textureElement.Kind, textureElement.Stages);

            description.Elements[^1] = TEXTURE_LAYOUT.Elements.Single(e => e.Kind == ResourceKind.Sampler);
            return texture_layouts[textureCount] = Factory.CreateResourceLayout(description);
        }

        #endregion

        #region Shaders

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

            pipelineDescription.ShaderSet.Shaders = shader.Shaders;

            currentShader = shader;
        }

        internal static void UpdateUniform<T>(IUniformWithValue<T> uniform)
            where T : struct, IEquatable<T>
        {
            if (uniform.Owner == currentShader)
                FlushCurrentBatch();

            switch (uniform)
            {
                case IUniformWithValue<Matrix3> matrix3:
                {
                    ref var value = ref matrix3.GetValueByRef();
                    updateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 0, ref value.Row0);
                    updateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 16, ref value.Row1);
                    updateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 32, ref value.Row2);
                    break;
                }

                case IUniformWithValue<Matrix4> matrix4:
                {
                    ref var value = ref matrix4.GetValueByRef();
                    updateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 0, ref value.Row0);
                    updateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 16, ref value.Row1);
                    updateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 32, ref value.Row2);
                    updateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 48, ref value.Row3);
                    break;
                }

                default:
                    updateBuffer(uniform.Owner.UniformBuffer, uniform.Location, ref uniform.GetValueByRef());
                    break;
            }
        }

        public static ResourceSet CreateUniformResourceSet(DeviceBuffer buffer) => Factory.CreateResourceSet(new ResourceSetDescription(uniformLayout, buffer));

        private static void updateBuffer<T>(DeviceBuffer buffer, int location, ref T value)
            where T : struct, IEquatable<T>
        {
            int size = Marshal.SizeOf<T>();

            var staging = StagingBufferPool.Get(size);
            Device.UpdateBuffer(staging, 0, ref value);
            Commands.CopyBuffer(staging, 0, buffer, (uint)location, (uint)size);
        }

        #endregion
    }
}
