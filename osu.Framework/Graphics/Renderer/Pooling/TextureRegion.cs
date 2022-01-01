// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid;

namespace osu.Framework.Graphics.Renderer.Pooling
{
    /// <summary>
    /// Represents a texture region returned during renting for usage.
    /// </summary>
    public readonly struct TextureRegion
    {
        /// <summary>
        /// The <see cref="Texture"/> resource in which this region lies under.
        /// </summary>
        public Texture Texture { get; }

        /// <summary>
        /// The X coordinate of this region.
        /// </summary>
        public uint X { get; }

        /// <summary>
        /// The Y coordinate of this region.
        /// </summary>
        public uint Y { get; }

        /// <summary>
        /// The width of this region.
        /// </summary>
        public uint Width { get; }

        /// <summary>
        /// The height of this region.
        /// </summary>
        public uint Height { get; }

        public TextureRegion(Texture texture, uint x, uint y, uint width, uint height)
        {
            Texture = texture;

            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
