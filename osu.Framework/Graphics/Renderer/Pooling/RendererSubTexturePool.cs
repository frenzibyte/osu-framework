// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using Veldrid;

namespace osu.Framework.Graphics.Renderer.Pooling
{
    // todo: use TextureAtlas and RendererTexture rather than reinventing the wheel.
    internal class RendererSubTexturePool : RendererPool<RendererSubTexturePool.Request, TextureRegion>, IDisposable
    {
        public readonly Texture Texture;

        public RendererSubTexturePool(Texture texture, string name)
            : base(name)
        {
            Texture = texture;
        }

        /// <summary>
        /// The current position for allocating a new texture region.
        /// </summary>
        private Vector2I currentPosition = Vector2I.Zero;

        /// <summary>
        /// The next empty row of the texture.
        /// </summary>
        private int nextEmptyRow;

        /// <summary>
        /// Whether this pool can allocate a new region with the specified size.
        /// </summary>
        /// <param name="width">The width to check with.</param>
        /// <param name="height">The height to check with.</param>
        public bool CanAllocateRegion(int width, int height) => (currentPosition.X + width <= Texture.Width && currentPosition.Y + height <= Texture.Height) || (width <= Texture.Width && nextEmptyRow + height <= Texture.Height);

        /// <summary>
        /// Whether the specified size will make the pool reach its end.
        /// </summary>
        /// <param name="width">The width to check with.</param>
        /// <param name="height">The height to check with.</param>
        public bool ReachesPoolEnd(int width, int height) => (currentPosition.X + width == Texture.Width && currentPosition.Y + height == Texture.Height) || (width == Texture.Width && nextEmptyRow + height == Texture.Height);

        /// <summary>
        /// Returns a <see cref="TextureRegion"/> from the pool.
        /// </summary>
        /// <param name="width">The texture region width.</param>
        /// <param name="height">The texture region height.</param>
        public TextureRegion Get(int width, int height) => base.Get(new Request { Width = width, Height = height });

        protected override bool CanUseResource(Request request, TextureRegion region) => region.Width >= request.Width && region.Height >= request.Height;

        protected override TextureRegion CreateResource(Request request)
        {
            if (currentPosition.X + request.Width > Texture.Width)
                currentPosition = new Vector2I(0, nextEmptyRow);

            if (!CanAllocateRegion(request.Width, request.Height))
                return default;

            nextEmptyRow = Math.Max(nextEmptyRow, currentPosition.Y + request.Height);

            var region = new TextureRegion(Texture, (uint)currentPosition.X, (uint)currentPosition.Y, (uint)request.Width, (uint)request.Height);
            currentPosition.X += request.Width;
            return region;
        }

        public override bool FreeUnusedResources(ulong resourceFreeInterval)
        {
            if (!(base.FreeUnusedResources(resourceFreeInterval)))
                return false;

            uint maxY = 0;

            foreach (var available in AvailableResources)
                maxY = Math.Max(maxY, available.resource.Y + available.resource.Height);

            nextEmptyRow = (int)Math.Min(nextEmptyRow, maxY);
            currentPosition = new Vector2I(0, nextEmptyRow);
            return true;
        }

        public void Dispose()
        {
            Texture?.Dispose();
        }

        internal struct Request
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }
}
