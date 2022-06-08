// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Allocation;
using osuTK;

namespace SampleGame
{
    public class SampleGameGame : Game
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(new AudioLatencyTester
            {
                Scale = new Vector2(2f),
                Size = new Vector2(0.5f),
            });
        }
    }
}
