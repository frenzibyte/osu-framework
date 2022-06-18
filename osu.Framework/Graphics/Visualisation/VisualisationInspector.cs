// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Visualisation
{
    internal abstract class VisualisationInspector : VisibilityContainer
    {
        private const float width = 600;

        [Cached]
        public Bindable<Drawable> InspectedDrawable { get; private set; } = new Bindable<Drawable>();

        protected VisualisationInspector()
        {
            Width = width;
            RelativeSizeAxes = Axes.Y;
        }

        protected override void PopIn()
        {
            this.ResizeWidthTo(width, 500, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.ResizeWidthTo(0, 500, Easing.OutQuint);
        }
    }
}
