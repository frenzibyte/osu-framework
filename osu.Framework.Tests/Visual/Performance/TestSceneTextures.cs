// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Performance
{
    [Description("tests mipmapping gains")]
    public partial class TestSceneTextures : TestSceneBoxes
    {
        private float fillWidth;
        private float fillHeight;
        private bool disableMipmaps;
        private bool gradientColour;
        private bool randomiseColour;

        private Texture nonMipmappedSampleTexture = null!;
        private Texture mipmappedSampleTexture = null!;

        [BackgroundDependencyLoader]
        private void load(Game game, TextureStore store, IRenderer renderer, GameHost host)
        {
            mipmappedSampleTexture = store.Get(@"sample-texture");
            nonMipmappedSampleTexture = new TextureStore(renderer, host.CreateTextureLoaderStore(game.Resources), manualMipmaps: true).Get(@"Textures/sample-texture");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddToggleStep("disable mipmaps", v => disableMipmaps = v);
        }

        protected override Drawable CreateDrawable()
        {
            var sprite = base.CreateDrawable();

            return new Sprite
            {
                Texture = disableMipmaps ? nonMipmappedSampleTexture : mipmappedSampleTexture,
                Colour = sprite.Colour,
                RelativeSizeAxes = sprite.RelativeSizeAxes,
                Size = sprite.Size,
            };
        }
    }
}
