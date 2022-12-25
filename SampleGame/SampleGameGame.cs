// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics.ES30;
using Color4 = osuTK.Graphics.Color4;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            Host.DrawThread.Scheduler.Add(() =>
            {
                int maxTextureSize = GL.GetInteger(GetPName.MaxTextureSize);

                FillFlowContainer fillFlowContainer = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Scale = new Vector2(2f),
                    Children = new[]
                    {
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = $"Maximum texture size: {maxTextureSize} / {Host.Renderer.WhitePixel.GetTextureRect().Width}"
                        },
                    }
                };

                Schedule(() => Add(fillFlowContainer));

                foreach (var type in new[] { ShaderType.VertexShader, ShaderType.FragmentShader })
                {
                    foreach (var precision in new[]
                                 { ShaderPrecision.HighFloat, ShaderPrecision.HighInt, ShaderPrecision.MediumFloat, ShaderPrecision.MediumInt, ShaderPrecision.LowFloat, ShaderPrecision.LowInt })
                    {
                        int[] range = new int[2];
                        int precisionValue;

                        GL.GetShaderPrecisionFormat(type, precision, range, out precisionValue);

                        Schedule(() => fillFlowContainer.Add(new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = $"{type}: {precision} shader precision: {precisionValue} ({range[0]}, {range[1]})"
                        }));
                    }
                }

                Schedule(() =>
                {
                    AddInternal(new FillFlowContainer
                    {
                        Depth = int.MaxValue,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Children = new Drawable[]
                        {
                            new CircularBlob
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Amplitude = 0.5f, InnerRadius = 0.25f, Size = new Vector2(200), Colour = Color4.Gray
                            },
                            new CircularProgress
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Current = { Value = 1 }, InnerRadius = 0.25f, Size = new Vector2(200), Colour = Color4.Gray
                            },
                            new BasicHSVColourPicker
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Width = 200
                            },
                        }
                    });
                });
            });
        }
    }
}
