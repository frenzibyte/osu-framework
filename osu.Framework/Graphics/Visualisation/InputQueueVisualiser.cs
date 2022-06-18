// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;

namespace osu.Framework.Graphics.Visualisation
{
    internal class InputQueueVisualiser : Container, IContainVisualisedDrawables
    {
        private readonly InputQueueType type;

        private FillFlowContainer<VisualisedDrawable> flow = null!;

        public Bindable<Drawable> InspectedDrawable { get; } = new Bindable<Drawable>();

        public InputQueueVisualiser(InputQueueType type)
        {
            this.type = type;

            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new BasicScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = flow = new FillFlowContainer<VisualisedDrawable>
                {
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                }
            };
        }

        protected override void Update()
        {
            base.Update();

            if (InspectedDrawable.Value == null)
                return;

            var inputManager = (InputManager)InspectedDrawable.Value;

            var queue = type == InputQueueType.Positional
                ? inputManager.PositionalInputQueue
                : inputManager.NonPositionalInputQueue;

            flow.Clear(false);

            foreach (var drawable in queue)
                getVisualiserFor(drawable).SetContainer(this);
        }

        [Resolved]
        private Game game { get; set; } = null!;

        public void AddVisualiser(VisualisedDrawable visualiser)
        {
            visualiser.RequestTarget += _ => game.ShowDrawVisualiser(visualiser.Target);
            visualiser.Depth = 0;

            flow.Add(visualiser);
        }

        public void RemoveVisualiser(VisualisedDrawable visualiser) => flow.Remove(visualiser);

        private readonly Dictionary<Drawable, VisualisedDrawable> visCache = new Dictionary<Drawable, VisualisedDrawable>();

        private VisualisedDrawable getVisualiserFor(Drawable drawable)
        {
            if (visCache.TryGetValue(drawable, out var existing))
                return existing;

            var vis = new VisualisedDrawable(drawable, false);
            vis.OnDispose += () => visCache.Remove(vis.Target);

            return visCache[drawable] = vis;
        }
    }
}
