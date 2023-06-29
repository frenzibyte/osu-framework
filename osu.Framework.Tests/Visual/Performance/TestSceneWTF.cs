// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneWTF : FrameworkTestScene
    {
        private Box box = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Spacing = new Vector2(20f),
                ChildrenEnumerable = Enumerable.Range(0, 250).Select(b => new TestSprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(20f),
                })
            });
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

                public override void Draw(IRenderer renderer)
                {
                    base.Draw(renderer);
                    renderer.FlushCurrentBatch(FlushBatchSource.SomethingElse);
                }
            }
        }
    }
}
