// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics.Renderer;
using osu.Framework.Platform;

namespace SampleGame.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableDesktopHost(@"sample-game"))
            {
                VeldridGraphicsBackend.Host = host;

                using (Game game = new SampleGameGame())
                    host.Run(game);
            }
        }
    }
}
