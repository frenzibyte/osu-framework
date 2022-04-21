// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Graphics;

namespace osu.Framework.Graphics.Veldrid
{
    /// <summary>
    /// Information for how the current frame buffer should be cleared.
    /// </summary>
    public readonly struct ClearInfo
    {
        /// <summary>
        /// The default clear properties.
        /// </summary>
        public static ClearInfo Default => new ClearInfo(Color4.Black);

        /// <summary>
        /// The colour to write to the frame buffer.
        /// </summary>
        public readonly Colour4 Colour;

        /// <summary>
        /// The depth to write to the frame buffer.
        /// </summary>
        public readonly double Depth;

        /// <summary>
        /// The stencil value to write to the frame buffer.
        /// </summary>
        public readonly int Stencil;

        public ClearInfo(Colour4 colour, double depth = 1f, int stencil = 0)
        {
            Colour = colour;
            Depth = depth;
            Stencil = stencil;
        }
    }
}
