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
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Shader = osu.Framework.Graphics.Shaders.Shader;

namespace osu.Framework.Graphics.Renderer
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

            using (BeginCommands())
                UpdateTexture(defaultTexture, 0, 0, 1, 1, 0, new ReadOnlySpan<Rgba32>(new[] { new Rgba32(0, 0, 0) }));

            defaultTextureSet = new TextureResourceSet(defaultTexture, Device.LinearSampler);
        }

        #region Buffers

        private static DeviceBuffer boundVertexBuffer;
        private static VertexLayoutDescription boundVertexLayout;

        /// <summary>
        /// Binds a vertex buffer with the specified <see cref="VertexLayoutDescription"/> to the <see cref="Commands"/> list.
        /// </summary>
        /// <param name="buffer">The vertex buffer.</param>
        /// <param name="layout">The vertex layout.</param>
        /// <returns>Whether the vertex buffer has been bound, otherwise the vertex buffer has already been bound.</returns>
        public static bool BindVertexBuffer(DeviceBuffer buffer, VertexLayoutDescription layout)
        {
            if (buffer == boundVertexBuffer)
                return false;

            Commands.SetVertexBuffer(0, buffer);

            if (currentShader.VertexLayout.Elements == null || currentShader.VertexLayout.Elements.Length == 0)
                pipelineDescription.ShaderSet.VertexLayouts = new[] { layout };

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);

            boundVertexBuffer = buffer;
            boundVertexLayout = layout;
            return true;
        }

        /// <summary>
        /// Binds an index buffer with the specified <see cref="IndexFormat"/> to the <see cref="Commands"/> list.
        /// </summary>
        /// <param name="buffer">The index buffer.</param>
        /// <param name="format">The index format.</param>
        public static void BindIndexBuffer(DeviceBuffer buffer, IndexFormat format) => Commands.SetIndexBuffer(buffer, format);

        /// <summary>
        /// Updates a <see cref="DeviceBuffer"/> region with the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="DeviceBuffer"/> to update.</param>
        /// <param name="offset">The offset of the update region in bytes.</param>
        /// <param name="value">The value to upload.</param>
        /// <param name="size">The value size in bytes, otherwise the value type size will be used.</param>
        /// <typeparam name="T">The value type.</typeparam>
        public static void UpdateBuffer<T>(DeviceBuffer buffer, int offset, ref T value, int? size = null)
            where T : unmanaged, IEquatable<T>
        {
            size ??= Marshal.SizeOf<T>();

            var staging = staging_buffer_pool.Get(size.Value);
            Device.UpdateBuffer(staging, 0, ref value, (uint)size);
            Commands.CopyBuffer(staging, 0, buffer, (uint)offset, (uint)size);
        }

        #endregion

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
        /// Unbinds any bound texture set and binds a default texture set.
        /// </summary>
        internal static void BindDefaultTexture() => BindTexture(defaultTextureSet);

        /// <summary>
        /// Updates a <see cref="Texture"/> with a <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The textural data.</param>
        /// <typeparam name="T">The pixel type.</typeparam>
        public static unsafe void UpdateTexture<T>(Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data)
            where T : unmanaged
        {
            var staging = staging_texture_pool.Get(width, height, texture.Format);

            fixed (T* ptr = data)
                Device.UpdateTexture(staging.Texture, (IntPtr)ptr, (uint)(data.Length * sizeof(T)), staging.X, staging.Y, 0, (uint)width, (uint)height, 1, 0, 0);

            // Logger.Log($"Blitting from {x}x{y} to {width}x{height} textural data at level {level} to a texture with {texture.Width}x{texture.Height} dimensions.", LoggingTarget.Runtime, LogLevel.Important);
            Commands.CopyTexture(staging.Texture, staging.X, staging.Y, 0, 0, 0, texture, (uint)x, (uint)y, 0, (uint)level, 0, (uint)width, (uint)height, 1, 1);
        }

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

            if (shader.VertexLayout.Elements?.Length > 0)
                pipelineDescription.ShaderSet.VertexLayouts = new[] { shader.VertexLayout };

            currentShader = shader;
        }

        internal static void UpdateUniform<T>(IUniformWithValue<T> uniform)
            where T : unmanaged, IEquatable<T>
        {
            if (uniform.Owner == currentShader)
                FlushCurrentBatch();

            switch (uniform)
            {
                case IUniformWithValue<Matrix3> matrix3:
                {
                    ref var value = ref matrix3.GetValueByRef();
                    UpdateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 0, ref value.Row0);
                    UpdateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 16, ref value.Row1);
                    UpdateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 32, ref value.Row2);
                    break;
                }

                case IUniformWithValue<Matrix4> matrix4:
                {
                    ref var value = ref matrix4.GetValueByRef();
                    UpdateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 0, ref value.Row0);
                    UpdateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 16, ref value.Row1);
                    UpdateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 32, ref value.Row2);
                    UpdateBuffer(uniform.Owner.UniformBuffer, uniform.Location + 48, ref value.Row3);
                    break;
                }

                default:
                    UpdateBuffer(uniform.Owner.UniformBuffer, uniform.Location, ref uniform.GetValueByRef());
                    break;
            }
        }

        public static ResourceSet CreateUniformResourceSet(DeviceBuffer buffer) => Factory.CreateResourceSet(new ResourceSetDescription(uniformLayout, buffer));

        #endregion
    }
}
