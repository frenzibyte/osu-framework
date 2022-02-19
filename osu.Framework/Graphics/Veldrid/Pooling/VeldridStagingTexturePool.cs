// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Pooling
{
    internal class VeldridStagingTexturePool : VeldridPool<VeldridStagingTexturePool.Request, VeldridSubTexturePool>
    {
        private const int min_texture_pool_size = 1024;

        public VeldridStagingTexturePool()
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

        protected override bool CanUseResource(Request request, VeldridSubTexturePool pool)
        {
            var size = getRecommendedSizeFor(request);
            if (pool.Texture.Width != size.Width && pool.Texture.Height != size.Height)
                return false;

            return pool.Texture.Format == request.Format && pool.CanAllocateRegion(request.Width, request.Height);
        }

        protected override bool CanResourceRemainAvailable(Request request, VeldridSubTexturePool pool) => !pool.ReachesPoolEnd(request.Width, request.Height);

        protected override VeldridSubTexturePool CreateResource(Request request)
        {
            var size = getRecommendedSizeFor(request);
            var description = TextureDescription.Texture2D((uint)size.Width, (uint)size.Height, 1, 1, request.Format, TextureUsage.Staging);
            var texture = Vd.Factory.CreateTexture(description);
            return new VeldridSubTexturePool(texture, "Staging Texture Regions");
        }

        private static Size getRecommendedSizeFor(Request request) => new Size
        {
            Width = (int)Math.Max(Math.Pow(2, Math.Ceiling(Math.Log(request.Width, 2))), min_texture_pool_size),
            Height = (int)Math.Max(Math.Pow(2, Math.Ceiling(Math.Log(request.Height, 2))), min_texture_pool_size),
        };

        internal struct Request
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public PixelFormat Format { get; set; }
        }
    }
}