// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneSpriteText : FrameworkTestScene
    {
        public TestSceneSpriteText()
        {
            FillFlowContainer flow;

            Children = new Drawable[]
            {
                new BasicScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        flow = new FillFlowContainer
                        {
                            Anchor = Anchor.TopLeft,
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Vertical,
                        }
                    }
                }
            };

            flow.Add(new SpriteText
            {
                Text = @"the quick red fox jumps over the lazy brown dog"
            });
            flow.Add(new SpriteText
            {
                Text = @"THE QUICK RED FOX JUMPS OVER THE LAZY BROWN DOG"
            });
            flow.Add(new SpriteText
            {
                Text = @"0123456789!@#$%^&*()_-+-[]{}.,<>;'\"
            });
            flow.Add(new Container
            {
                Margin = new MarginPadding(15f),
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = FrameworkColour.GreenDark,
                    },
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Red,
                        RelativeSizeAxes = Axes.X,
                        Height = 1f,
                    },
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Red,
                        RelativeSizeAxes = Axes.Y,
                        Width = 1f,
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Margin = new MarginPadding(20f),
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = FrameworkColour.Green,
                            },
                            new SpriteText
                            {
                                Text = "pA",
                                UseFullGlyphHeight = false,
                            }
                        }
                    }
                }
            });

            for (int i = 1; i <= 200; i++)
            {
                SpriteText text = new SpriteText
                {
                    Text = $@"Font testy at size {i}",
                    Font = new FontUsage("Roboto", i, i % 4 > 1 ? "Bold" : "Regular", i % 2 == 1),
                    AllowMultiline = true,
                    RelativeSizeAxes = Axes.X,
                };

                flow.Add(text);
            }
        }
    }
}
