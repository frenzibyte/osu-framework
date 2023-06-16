// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        private Box box = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(150, 150),
                Colour = Color4.Tomato
            });
        }

        private bool first = true;

        protected override void Update()
        {
            base.Update();
            box.Rotation = 0;

            if (first)
            {
                box.MoveTo(new Vector2(DrawWidth, 0))
                   .MoveTo(new Vector2(0f, 0f), 1000)
                   .Loop();

                first = false;
            }
        }
    }
}
