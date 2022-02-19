// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Veldrid.Textures;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture which can cleans up any resources held by the underlying <see cref="VeldridTexture"/> on <see cref="Dispose"/>.
    /// </summary>
    public class DisposableTexture : Texture
    {
        public DisposableTexture(VeldridTexture veldridTexture)
            : base(veldridTexture)
        {
        }

        public DisposableTexture(int width, int height, bool manualMipmaps = false, FilteringMode filteringMode = FilteringMode.Linear)
            : base(width, height, manualMipmaps, filteringMode)
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            VeldridTexture.Dispose();
        }
    }
}
