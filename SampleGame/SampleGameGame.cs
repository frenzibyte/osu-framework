// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics.ES30;

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
            });
        }
    }
}
