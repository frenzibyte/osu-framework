// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Textures;
using osu.Framework.Graphics.Rendering.Vertices;
using Veldrid;
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
            => factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, (uint)Math.Min(maximumLevels, calculateMipmapLevels(width, height)), 1, format, TextureUsage.Sampled));

        public IDisposable CreateVertexBuffer(int length)
        {
            throw new NotImplementedException();
        }

        public IDisposable CreateIndexBuffer(int length)
        {
            throw new NotImplementedException();
        }

        public IDisposable CreateVertexShader(byte[] bytes, out VertexLayoutElement[] elements)
        {
            throw new NotImplementedException();
        }

        public IDisposable CreateFragmentShader(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public IDisposable CreateFrameBuffer(RendererTexture target, PixelFormat[] renderFormats, PixelFormat? depthFormat = null)
        {
            throw new NotImplementedException();
        }

        private static int calculateMipmapLevels(int width, int height) => 1 + (int)Math.Floor(Math.Log(Math.Max(width, height), 2));
    }
}
