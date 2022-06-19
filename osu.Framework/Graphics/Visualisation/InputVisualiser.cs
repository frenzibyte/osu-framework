// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;

namespace osu.Framework.Graphics.Visualisation
{
    [Cached]
    internal class InputVisualiser : VisualisationToolWindow
    {
        private readonly Bindable<Drawable> selectedDrawable = new Bindable<Drawable>();

        public InputVisualiser()
            : base("Input Queue", "(Ctrl+F4 to toggle)")
        {
        }

        protected override void OnTargetSelected(Drawable target, Drawable validTarget)
        {
            selectedDrawable.Value = target;

            if (Inspector.State.Value == Visibility.Hidden)
                ToggleInspector();
        }

        protected override VisualisationInspector CreateInspector() => new InputInspector
        {
            SelectedDrawable = { BindTarget = selectedDrawable },
        };

        protected override bool ValidForVisualisation(Drawable drawable) => drawable is InputManager;
    }
}
