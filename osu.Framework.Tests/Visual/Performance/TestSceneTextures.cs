// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Performance
{
    [Description("tests mipmapping gains")]
    public partial class TestSceneTextures : TestSceneBoxes
    {
        private float fillWidth;
        private float fillHeight;
        private bool disableMipmaps;
        private bool uniqueTextures;
        private bool gradientColour;
        private bool randomiseColour;

        private Texture nonMipmappedSampleTexture = null!;
        private Texture mipmappedSampleTexture = null!;

        private IResourceStore<TextureUpload> textureLoaderStore = null!;

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(Game game, TextureStore store, GameHost host)
        {
            textureLoaderStore = host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, @"Textures"));

            mipmappedSampleTexture = store.Get(@"sample-texture");
            nonMipmappedSampleTexture = new TextureStore(renderer, textureLoaderStore, manualMipmaps: true).Get(@"sample-texture");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddToggleStep("disable mipmaps", v => disableMipmaps = v);
            AddToggleStep("unique textures", v => uniqueTextures = v);
        }

        protected override Drawable CreateDrawable()
        {
            var baseSprite = base.CreateDrawable();

            var sprite = new Sprite
            {
                Colour = baseSprite.Colour,
                RelativeSizeAxes = baseSprite.RelativeSizeAxes,
                Size = baseSprite.Size,
            };

            if (uniqueTextures)
                sprite.Texture = new TextureStore(renderer, textureLoaderStore, manualMipmaps: disableMipmaps).Get(@"sample-texture");
            else
                sprite.Texture = disableMipmaps ? nonMipmappedSampleTexture : mipmappedSampleTexture;

            return sprite;
        }

        private partial class TestSprite : Sprite
        {
            protected override DrawNode CreateDrawNode() => new TestSpriteDrawNode(this);

            private class TestSpriteDrawNode : SpriteDrawNode
            {
                public TestSpriteDrawNode(Sprite source)
                    : base(source)
                {
                }

                public override void Draw(IRenderer renderer)
                {
                    base.Draw(renderer);
                    renderer.FlushCurrentBatch(FlushBatchSource.SomethingElse);
                }
            }
        }
    }
}
