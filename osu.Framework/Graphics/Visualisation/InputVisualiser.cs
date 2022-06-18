// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Input;

namespace osu.Framework.Graphics.Visualisation
{
    [Cached]
    internal class InputVisualiser : ToolWindow, IContainVisualisedDrawables, IRequireHighFrequencyMousePosition
    {
        public InputVisualiser()
            : base("Input Queue", "(Ctrl+F4 to toggle)")
        {
        }

        public void AddVisualiser(VisualisedDrawable visualiser)
        {
        }

        public void RemoveVisualiser(VisualisedDrawable visualiser)
        {
        }
    }
}
