// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Timing;

namespace osu.Framework.Screens
{
    public partial class Screen : Container, IScreen
    {
        public bool ValidForResume { get; set; } = true;

        public bool ValidForPush { get; set; } = true;

        public sealed override bool RemoveWhenNotAlive => false;

        [Resolved]
        protected Game Game { get; private set; } = null!;

        protected override Container<Drawable> Content { get; }

        public Screen()
        {
            RelativeSizeAxes = Axes.Both;

            base.AddInternal(Content = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });
        }

        protected sealed override void AddInternal(Drawable drawable) => throw new InvalidOperationException($"Use {nameof(Add)} or {nameof(Content)} instead.");

        internal override void UpdateClock(IFrameBasedClock clock)
        {
            base.UpdateClock(clock);
            if (Parent != null && !(Parent is ScreenStack))
                throw new InvalidOperationException($"Screens must always be added to a {nameof(ScreenStack)} (attempted to add {GetType()} to {Parent.GetType()})");
        }

        public virtual void OnEntering(ScreenTransitionEvent e) { }

        public virtual bool OnExiting(ScreenExitEvent e) => false;

        public virtual void OnResuming(ScreenTransitionEvent e) { }

        public virtual void OnSuspending(ScreenTransitionEvent e) { }
    }
}
