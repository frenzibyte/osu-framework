// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new MyDrawable());
        }
    }
}
