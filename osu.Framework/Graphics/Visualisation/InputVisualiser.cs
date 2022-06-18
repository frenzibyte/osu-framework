// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.Visualisation
{
    [Cached]
    internal class InputVisualiser : VisualisationToolWindow
    {
        public InputVisualiser()
            : base("Input Queue", "(Ctrl+F4 to toggle)")
        {
        }

        protected override bool OnClick(ClickEvent e)
        {
            bool found = base.OnClick(e);

            if (found && Inspector.State.Value == Visibility.Hidden)
                ToggleInspector();

            return found;
        }

        protected override VisualisationInspector CreateInspector() => new InputInspector();

        protected override bool ValidForVisualisation(Drawable drawable) => drawable is InputManager;
    }
}
