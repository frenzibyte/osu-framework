// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Textures
{
    public enum PixelFormat
    {
        /// <summary>
        /// 32-bit unsigned normalized BGRA channel.
        /// </summary>
        BGRA,

        /// <summary>
        /// 32-bit unsigned normalized RGBA channel in sRGB format.
        /// </summary>
        RGBA_Srgb,

        /// <summary>
        /// 8-bit single channel.
        /// </summary>
        Red,
    }
}
