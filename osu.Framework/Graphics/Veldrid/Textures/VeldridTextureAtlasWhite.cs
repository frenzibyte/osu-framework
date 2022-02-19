// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    /// <summary>
    /// A special texture which refers to the area of a texture atlas which is white.
    /// Allows use of such areas while being unaware of whether we need to bind a texture or not.
    /// </summary>
    internal class VeldridTextureAtlasWhite : VeldridTextureSub
    {
        public VeldridTextureAtlasWhite(VeldridTextureSingle parent)
            : base(new RectangleI(0, 0, 1, 1), parent)
        {
            Opacity = Opacity.Opaque;
        }

        internal override bool Bind(WrapMode wrapModeS, WrapMode wrapModeT)
        {
            //we can use the special white space from any atlas texture.
            if (Vd.AtlasTextureIsBound)
                return true;

            return base.Bind(wrapModeS, wrapModeT);
        }
    }
}
