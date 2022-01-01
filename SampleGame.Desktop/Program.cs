// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics.Renderer;

namespace SampleGame.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (var host = Host.GetSuitableHost("osu-framework-veldrid"))
            {
                VeldridGraphicsBackend.Host = host;
                host.Run(new SampleGameGame());
            }
        }
    }
}
