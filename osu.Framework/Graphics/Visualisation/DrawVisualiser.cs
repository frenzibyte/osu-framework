// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.Visualisation
{
    internal class DrawVisualiser : VisualisationToolWindow
    {
        public DrawVisualiser()
            : base("Draw Visualiser", "(Ctrl+F1 to toggle)")
        {
        }

        protected override VisualisationInspector CreateInspector() => new DrawableInspector();
    }
}
