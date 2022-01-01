// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;
using Vd = osu.Framework.Graphics.Renderer.VeldridGraphicsBackend;

namespace osu.Framework.Graphics.Renderer.Pooling
{
    internal class RendererStagingTexturePool : RendererPool<RendererStagingTexturePool.Request, RendererSubTexturePool>
    {
        private const int pool_texture_size = 1024;

        public RendererStagingTexturePool()
            : base("Staging Textures")
        {
        }

        /// <summary>
        /// Returns a <see cref="TextureRegion"/> from the texture pools satisfying the specified request.
        /// </summary>
        /// <param name="width">The texture region width.</param>
        /// <param name="height">The texture region height.</param>
        /// <param name="format">The texture pixel format.</param>
        public TextureRegion Get(int width, int height, PixelFormat format)
        {
            var pool = base.Get(new Request { Width = width, Height = height, Format = format });
            return pool.Get(width, height);
        }

        protected override bool CanReuseResource(Request request, RendererSubTexturePool pool) => pool.Texture.Format == request.Format && pool.CanAllocateRegion(request.Width, request.Height);

        protected override bool IsResourceStillAvailable(RendererSubTexturePool pool) => pool.HasAvailableSpace;

        protected override RendererSubTexturePool CreateResource(Request request)
        {
            var description = TextureDescription.Texture2D((uint)Math.Max(request.Width, pool_texture_size), (uint)Math.Max(request.Height, pool_texture_size), 1, 1, request.Format, TextureUsage.Staging);
            var texture = Vd.Factory.CreateTexture(description);
            return new RendererSubTexturePool(texture, "Staging Texture Regions");
        }

        internal struct Request
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public PixelFormat Format { get; set; }
        }
    }
}
