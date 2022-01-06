// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Platform.Graphics.Veldrid;

namespace SampleGame.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (var host = Host.GetSuitableHost("osu-framework-veldrid"))
            {
                Renderer.Host = host;
                host.Run(new SampleGameGame());
            }
        }
    }
}
