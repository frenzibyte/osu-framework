// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Sprites
{
    public partial class SpriteText
    {
        internal class SpriteTextDrawNode : TexturedShaderDrawNode
        {
            protected new SpriteText Source => (SpriteText)base.Source;

            private bool shadow;
            private ColourInfo shadowColour;
            private Vector2 shadowOffset;

            private readonly List<ScreenSpaceCharacterPart> parts = new List<ScreenSpaceCharacterPart>();

            public SpriteTextDrawNode(SpriteText source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                parts.Clear();
                parts.AddRange(Source.screenSpaceCharacters);
                shadow = Source.Shadow;

                if (shadow)
                {
                    shadowColour = Source.ShadowColour;
                    shadowOffset = Source.premultipliedShadowOffset;
                }
            }

            public override void Draw(in DrawState drawState)
            {
                base.Draw(drawState);

                using (drawState.QuadBatch.BeginUsage(ref BatchUsage, this))
                {
                    Shader.Bind();

                    var avgColour = (Color4)DrawColourInfo.Colour.AverageColour;
                    float shadowAlpha = MathF.Pow(Math.Max(Math.Max(avgColour.R, avgColour.G), avgColour.B), 2);

                    //adjust shadow alpha based on highest component intensity to avoid muddy display of darker text.
                    //squared result for quadratic fall-off seems to give the best result.
                    var finalShadowColour = DrawColourInfo.Colour;
                    finalShadowColour.ApplyChild(shadowColour.MultiplyAlpha(shadowAlpha));

                    for (int i = 0; i < parts.Count; i++)
                    {
                        if (shadow)
                        {
                            var shadowQuad = parts[i].DrawQuad;

                            DrawQuad(parts[i].Texture,
                                new Quad(
                                    shadowQuad.TopLeft + shadowOffset,
                                    shadowQuad.TopRight + shadowOffset,
                                    shadowQuad.BottomLeft + shadowOffset,
                                    shadowQuad.BottomRight + shadowOffset),
                                finalShadowColour, ref BatchUsage, inflationPercentage: parts[i].InflationPercentage);
                        }

                        DrawQuad(parts[i].Texture, parts[i].DrawQuad, DrawColourInfo.Colour, ref BatchUsage, inflationPercentage: parts[i].InflationPercentage);
                    }

                    Shader.Unbind();
                }
            }
        }

        /// <summary>
        /// A character of a <see cref="SpriteText"/> provided with screen space draw coordinates.
        /// </summary>
        internal struct ScreenSpaceCharacterPart
        {
            /// <summary>
            /// The screen-space quad for the character to be drawn in.
            /// </summary>
            public Quad DrawQuad;

            /// <summary>
            /// Extra padding for the character's texture.
            /// </summary>
            public Vector2 InflationPercentage;

            /// <summary>
            /// The texture to draw the character with.
            /// </summary>
            public Texture Texture;
        }
    }
}
