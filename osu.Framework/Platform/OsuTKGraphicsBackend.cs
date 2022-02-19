// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using osuTK.Platform;
using Veldrid;

namespace osu.Framework.Platform
{
    public class OsuTKGraphicsBackend : IGraphicsBackend
    {
        private IGameWindow gameWindow;

        public GraphicsBackend Type => GraphicsBackend.OpenGLES;

        public void Initialise(IWindow window)
        {
            if (!(window is OsuTKWindow osuTKWindow))
                throw new InvalidOperationException($"The specified window is not of type {nameof(OsuTKWindow)}.");

            gameWindow = osuTKWindow;
        }

        public Size GetDrawableSize() => gameWindow.ClientSize;
    }
}
