// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color4 = osuTK.Graphics.Color4;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            FillFlowContainer fillFlow;

            AddInternal(fillFlow = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0f, 10f),
            });

            foreach (int size in new[] { 0, 64, 65, 66, 512, 1024, 4096, 16384 })
            {
                Texture texture;

                if (size == 0)
                {
                    const int width = 128;

                    var image = new Image<Rgba32>(width, 1);

                    texture = Host.Renderer.CreateTexture(width, 1, true);

                    for (int i = 0; i < width; ++i)
                    {
                        float brightness = (float)i / (width - 1);
                        image[i, 0] = new Rgba32((byte)(128 + (1 - brightness) * 127), (byte)(128 + brightness * 127), 128, 255);
                    }

                    texture.SetData(new TextureUpload(image));
                }
                else
                    texture = new TextureWhitePixel(((TextureWhitePixel)Host.Renderer.WhitePixel).Parent, size);

                fillFlow.Add(new FillFlowContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(10f, 0f),
                    Scale = new Vector2(1f),
                    Children = new Drawable[]
                    {
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = size == 0 ? "Custom" : size.ToString(),
                        },
                        new CircularBlob
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Amplitude = 0.5f, InnerRadius = 0.25f, Size = new Vector2(200), Colour = Color4.Gray,
                            Texture = texture,
                        },
                        new CircularProgress
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Current = { Value = 1 }, InnerRadius = 0.25f, Size = new Vector2(200), Colour = Color4.Gray,
                            Texture = texture,
                        },
                        new BasicHSVColourPicker
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Width = 200,
                            Texture = texture,
                        },
                    },
                });
            }
        }
    }
}
