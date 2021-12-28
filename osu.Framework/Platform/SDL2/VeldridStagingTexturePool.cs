// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Statistics;
using Veldrid;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;

namespace osu.Framework.Platform.SDL2
{
    /// <summary>
    /// A staging <see cref="Texture"/> pool for temporary consumption.
    /// </summary>
    internal static class VeldridStagingTexturePool
    {
        private static readonly List<Texture> available_textures = new List<Texture>();
        private static readonly List<Texture> used_textures = new List<Texture>();

        private static readonly GlobalStatistic<int> stat_available_count = GlobalStatistics.Get<int>("Veldrid pools", "Available staging textures");
        private static readonly GlobalStatistic<int> stat_used_count = GlobalStatistics.Get<int>("Veldrid pools", "Used staging textures");

        /// <summary>
        /// Returns a staging <see cref="Texture"/> from the pool with a specified minimum size.
        /// </summary>
        /// <param name="minimumWidth">The minimum width of the returned texture.</param>
        /// <param name="minimumHeight">The minimum height of the returned texture.</param>
        /// <param name="format">The pixel format of the returned texture.</param>
        public static Texture Get(int minimumWidth, int minimumHeight, PixelFormat format)
        {
            Texture texture = null;

            foreach (Texture t in available_textures)
            {
                if (format == t.Format && t.Width >= minimumWidth && t.Height >= minimumHeight)
                {
                    texture = t;
                    available_textures.Remove(t);
                    stat_available_count.Value--;
                    break;
                }
            }

            minimumWidth = Math.Max(256, minimumWidth);
            minimumHeight = Math.Max(256, minimumHeight);

            texture ??= Vd.Factory.CreateTexture(TextureDescription.Texture2D((uint)minimumWidth, (uint)minimumHeight, 1, 1, format, TextureUsage.Staging));

            used_textures.Add(texture);
            stat_used_count.Value++;
            return texture;
        }

        /// <summary>
        /// Releases all staging <see cref="Texture"/>s and mark them back as available.
        /// </summary>
        public static void Release()
        {
            available_textures.AddRange(used_textures);
            stat_available_count.Value = available_textures.Count;

            used_textures.Clear();
            stat_used_count.Value = 0;
        }
    }
}
