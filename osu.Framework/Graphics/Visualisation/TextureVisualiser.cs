// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Graphics.Veldrid.Vertices;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Visualisation
{
    internal class TextureVisualiser : ToolWindow
    {
        private readonly FillFlowContainer<TexturePanel> atlasFlow;
        private readonly FillFlowContainer<TexturePanel> textureFlow;

        public TextureVisualiser()
            : base("Textures", "(Ctrl+F3 to toggle)")
        {
            ScrollContent.Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = "Atlases",
                        Padding = new MarginPadding(5),
                        Font = FrameworkFont.Condensed.With(weight: "Bold")
                    },
                    atlasFlow = new FillFlowContainer<TexturePanel>
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Spacing = new Vector2(22),
                        Padding = new MarginPadding(10),
                    },
                    new SpriteText
                    {
                        Text = "Textures",
                        Padding = new MarginPadding(5),
                        Font = FrameworkFont.Condensed.With(weight: "Bold")
                    },
                    textureFlow = new FillFlowContainer<TexturePanel>
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Spacing = new Vector2(22),
                        Padding = new MarginPadding(10),
                    }
                }
            };
        }

        protected override void PopIn()
        {
            base.PopIn();

            foreach (var tex in VeldridTextureSingle.GetAllTextures())
                addTexture(tex);

            VeldridTextureSingle.TextureCreated += addTexture;
        }

        protected override void PopOut()
        {
            base.PopOut();

            atlasFlow.Clear();
            textureFlow.Clear();

            VeldridTextureSingle.TextureCreated -= addTexture;
        }

        private void addTexture(VeldridTextureSingle veldridTexture) => Schedule(() =>
        {
            var target = veldridTexture is VeldridTextureAtlas ? atlasFlow : textureFlow;

            if (target.Any(p => p.VeldridTexture == veldridTexture))
                return;

            target.Add(new TexturePanel(veldridTexture));
        });

        private class TexturePanel : CompositeDrawable
        {
            private readonly WeakReference<VeldridTextureSingle> textureReference;

            public VeldridTextureSingle VeldridTexture => textureReference.TryGetTarget(out var tex) ? tex : null;

            private readonly SpriteText titleText;
            private readonly SpriteText footerText;

            private readonly UsageBackground usage;

            public TexturePanel(VeldridTextureSingle veldridTexture)
            {
                textureReference = new WeakReference<VeldridTextureSingle>(veldridTexture);

                Size = new Vector2(100, 132);

                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        titleText = new SpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FrameworkFont.Regular.With(size: 16)
                        },
                        new Container
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                usage = new UsageBackground(textureReference)
                                {
                                    Size = new Vector2(100)
                                },
                            },
                        },
                        footerText = new SpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FrameworkFont.Regular.With(size: 16),
                        },
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                try
                {
                    var texture = VeldridTexture;

                    if (texture?.Available != true)
                    {
                        Expire();
                        return;
                    }

                    titleText.Text = $"{texture.Width}x{texture.Height} ";
                    footerText.Text = Precision.AlmostBigger(usage.AverageUsagesPerFrame, 1) ? $"{usage.AverageUsagesPerFrame:N0} binds" : string.Empty;
                }
                catch
                {
                }
            }
        }

        private class UsageBackground : Box, IHasTooltip
        {
            private readonly WeakReference<VeldridTextureSingle> textureReference;

            private ulong lastBindCount;

            public float AverageUsagesPerFrame { get; private set; }

            public UsageBackground(WeakReference<VeldridTextureSingle> textureReference)
            {
                this.textureReference = textureReference;
            }

            protected override DrawNode CreateDrawNode() => new UsageBackgroundDrawNode(this);

            private class UsageBackgroundDrawNode : SpriteDrawNode
            {
                protected new UsageBackground Source => (UsageBackground)base.Source;

                private ColourInfo drawColour;

                private WeakReference<VeldridTextureSingle> textureReference;

                public UsageBackgroundDrawNode(Box source)
                    : base(source)
                {
                }

                public override void ApplyState()
                {
                    base.ApplyState();

                    textureReference = Source.textureReference;
                }

                public override void Draw(Action<TexturedVertex2D> vertexAction)
                {
                    if (!textureReference.TryGetTarget(out var texture))
                        return;

                    if (!texture.Available)
                        return;

                    ulong delta = texture.BindCount - Source.lastBindCount;

                    Source.AverageUsagesPerFrame = Source.AverageUsagesPerFrame * 0.9f + delta * 0.1f;

                    drawColour = DrawColourInfo.Colour;
                    drawColour.ApplyChild(
                        Precision.AlmostBigger(Source.AverageUsagesPerFrame, 1)
                            ? Interpolation.ValueAt(Source.AverageUsagesPerFrame, Color4.DarkGray, Color4.Red, 0, 200)
                            : Color4.Transparent);

                    base.Draw(vertexAction);

                    // intentionally after draw to avoid counting our own bind.
                    Source.lastBindCount = texture.BindCount;
                }

                protected override void Blit(Action<TexturedVertex2D> vertexAction)
                {
                    if (!textureReference.TryGetTarget(out var texture))
                        return;

                    const float border_width = 4;

                    // border
                    DrawQuad(Texture, ScreenSpaceDrawQuad, drawColour, null, vertexAction);

                    var shrunkenQuad = ScreenSpaceDrawQuad.AABBFloat.Shrink(border_width);

                    // background
                    DrawQuad(Texture, shrunkenQuad, Color4.Black, null, vertexAction);

                    float aspect = (float)texture.Width / texture.Height;

                    if (aspect > 1)
                    {
                        float newHeight = shrunkenQuad.Height / aspect;

                        shrunkenQuad.Y += (shrunkenQuad.Height - newHeight) / 2;
                        shrunkenQuad.Height = newHeight;
                    }
                    else if (aspect < 1)
                    {
                        float newWidth = shrunkenQuad.Width / (1 / aspect);

                        shrunkenQuad.X += (shrunkenQuad.Width - newWidth) / 2;
                        shrunkenQuad.Width = newWidth;
                    }

                    // texture
                    texture.Bind();
                    DrawQuad(texture, shrunkenQuad, Color4.White, null, vertexAction);
                }

                protected internal override bool CanDrawOpaqueInterior => false;
            }

            public LocalisableString TooltipText
            {
                get
                {
                    if (!textureReference.TryGetTarget(out var texture))
                        return string.Empty;

                    return $"type: {texture.GetType().Name}, size: {(float)texture.GetByteSize() / 1024 / 1024:N2}mb";
                }
            }
        }
    }
}
