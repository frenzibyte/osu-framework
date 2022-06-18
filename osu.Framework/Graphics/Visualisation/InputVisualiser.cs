// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Input;

namespace osu.Framework.Graphics.Visualisation
{
    [Cached]
    internal class InputVisualiser : VisualisationToolWindow
    {
        public InputVisualiser()
            : base("Input Queue", "(Ctrl+F4 to toggle)")
        {
        }

        protected override VisualisationInspector CreateInspector() => new InputInspector();

        protected override bool ValidForVisualisation(Drawable drawable) => drawable is InputManager;
    }
}
