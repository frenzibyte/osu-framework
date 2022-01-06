// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Textures;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Represents a graphics factory for an <see cref="IGraphicsBackend"/>.
    /// </summary>
    public interface IGraphicsFactory
    {
        /// <summary>
        /// Creates a new texture.
        /// </summary>
        /// <param name="width">The texture width.</param>
        /// <param name="height">The texture height.</param>
        /// <param name="format">The texture pixel format.</param>
        /// <param name="maximumLevels">The maximum number of mipmap levels.</param>
        IDisposable CreateTexture(int width, int height, PixelFormat format, int maximumLevels);

        /// <summary>
        /// Creates a new vertex buffer.
        /// </summary>
        /// <param name="length">The buffer length in bytes.</param>
        IDisposable CreateVertexBuffer(int length);

        /// <summary>
        /// Creates a new index buffer.
        /// </summary>
        /// <param name="length">The buffer length in bytes.</param>
        IDisposable CreateIndexBuffer(int length);

        /// <summary>
        /// Creates a new vertex shader.
        /// </summary>
        /// <param name="bytes">The shader bytes.</param>
        /// <param name="elements">The resultant vertex layout elements.</param>
        IDisposable CreateVertexShader(byte[] bytes, out VertexLayoutElement[] elements);

        /// <summary>
        /// Creates a new fragment/pixel shader.
        /// </summary>
        /// <param name="bytes">The shader bytes.</param>
        IDisposable CreateFragmentShader(byte[] bytes);

        /// <summary>
        /// Creates a new framebuffer.
        /// </summary>
        /// <param name="target">The main render target.</param>
        /// <param name="renderFormats">The render formats of the framebuffer.</param>
        /// <param name="depthFormat">The depth format of the framebuffer, or null to disable depth on this framebuffer.</param>
        IDisposable CreateFrameBuffer(RendererTexture target, PixelFormat[] renderFormats, PixelFormat? depthFormat = null);
    }
}
