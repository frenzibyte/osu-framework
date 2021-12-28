// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;

namespace osu.Framework.Platform.SDL2
{
    /// <summary>
    /// A staging <see cref="Texture"/> pool for temporary consumption.
    /// </summary>
    internal class VeldridStagingTexturePool : VeldridPool<Texture>
    {
        public VeldridStagingTexturePool()
            : base("Staging Textures")
        {
        }

        /// <summary>
        /// Returns a staging <see cref="Texture"/> from the pool with a specified minimum size.
        /// </summary>
        /// <param name="minimumWidth">The minimum width of the returned texture.</param>
        /// <param name="minimumHeight">The minimum height of the returned texture.</param>
        /// <param name="format">The pixel format of the returned texture.</param>
        public Texture Get(int minimumWidth, int minimumHeight, PixelFormat format)
        {
            minimumWidth = Math.Max(256, minimumWidth);
            minimumHeight = Math.Max(256, minimumHeight);

            return Get(t => t.Format == format && t.Width >= minimumWidth && t.Height >= minimumHeight, () =>
            {
                var description = TextureDescription.Texture2D((uint)minimumWidth, (uint)minimumHeight, 1, 1, format, TextureUsage.Staging);
                return Vd.Factory.CreateTexture(description);
            });
        }
    }
}
