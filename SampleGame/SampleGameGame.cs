// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Spacing = new Vector2(20f, 20f),
                ChildrenEnumerable = Enumerable.Range(1, 500).Select(_ => new TestBox { Size = new Vector2(50f) }),
            });
        }

        private partial class TestBox : Box
        {
            protected override DrawNode CreateDrawNode() => new TestBoxDrawNode(this);

            private class TestBoxDrawNode : SpriteDrawNode
            {
                public TestBoxDrawNode(Sprite source)
                    : base(source)
                {
                }

                public override void Draw(IRenderer renderer)
                {
                    base.Draw(renderer);
                    renderer.FlushCurrentBatch(null);
                }
            }
        }
    }
}
