// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Framework.Screens;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        private Box box = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            // Add(new FillFlowContainer
            // {
            //     RelativeSizeAxes = Axes.Both,
            //     Anchor = Anchor.Centre,
            //     Origin = Anchor.Centre,
            //     Spacing = new Vector2(20f),
            //     ChildrenEnumerable = Enumerable.Range(0, 500).Select(b => new TestSprite
            //     {
            //         Anchor = Anchor.Centre,
            //         Origin = Anchor.Centre,
            //         Size = new Vector2(20f),
            //     })
            // });
            Add(new MyDrawable());
        }

        protected override void Update()
        {
            base.Update();
        }

        private partial class TestSprite : Box
        {
            protected override DrawNode CreateDrawNode() => new TestSpriteDrawNode(this);

            private class TestSpriteDrawNode : SpriteDrawNode
            {
                public TestSpriteDrawNode(Sprite source)
                    : base(source)
                {
                }

                private bool firstTime = true;

                public override void Draw(IRenderer renderer)
                {
                    base.Draw(renderer);

                    // Logger.Log(
                    //     $"new Quad(new Vector2({ScreenSpaceDrawQuad.TopLeft.X}, {ScreenSpaceDrawQuad.TopLeft.Y}), new Vector2({ScreenSpaceDrawQuad.TopRight.X}, {ScreenSpaceDrawQuad.TopRight.Y}), new Vector2({ScreenSpaceDrawQuad.BottomLeft.X}, {ScreenSpaceDrawQuad.BottomLeft.Y}), new Vector2({ScreenSpaceDrawQuad.BottomRight.X}, {ScreenSpaceDrawQuad.BottomRight.Y}))");

                    renderer.FlushCurrentBatch(FlushBatchSource.SomethingElse);
                }
            }
        }

        private partial class MyDrawable : Drawable, ITexturedShaderDrawable
        {
            public IShader? TextureShader { get; set; }

            [BackgroundDependencyLoader]
            private void load(ShaderManager shaders)
            {
                TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            }

            protected override DrawNode CreateDrawNode() => new MyDrawNode(this);

            private class MyDrawNode : TexturedShaderDrawNode
            {
                private readonly MyDrawable source;

                public MyDrawNode(MyDrawable source)
                    : base(source)
                {
                    this.source = source;
                }

                public override void Draw(IRenderer renderer)
                {
                    base.Draw(renderer);

                    BindTextureShader(renderer);

                    foreach (var quad in source.quads)
                    {
                        renderer.DrawQuad(renderer.WhitePixel, new Quad(quad.TopLeft / 1.2f, quad.TopRight / 1.2f, quad.BottomLeft / 1.2f, quad.BottomRight / 1.2f), Color4.White);
                        renderer.FlushCurrentBatch(FlushBatchSource.SomethingElse);
                    }

                    UnbindTextureShader(renderer);
                }
            }
        }
    }
}
