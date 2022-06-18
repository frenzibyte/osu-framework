// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.Visualisation
{
    internal class TreeContainer : ToolWindow
    {
        private readonly SpriteText waitingText;

        public Action ChooseTarget;
        public Action GoUpOneParent;
        public Action ToggleInspector;

        [Resolved]
        private VisualisationToolWindow visualiser { get; set; }

        public VisualisedDrawable Target
        {
            set
            {
                if (value == null)
                    ScrollContent.Clear(false);
                else
                    ScrollContent.Child = value;
            }
        }

        private VisualisationInspector inspector;

        public VisualisationInspector Inspector
        {
            get => inspector;
            set
            {
                if (inspector == value)
                    return;

                if (inspector != null)
                    MainHorizontalContent.Remove(inspector);

                inspector = value;
                MainHorizontalContent.Add(value);
            }
        }

        public TreeContainer(string title, string keyHelpText, bool hasInspector = false)
            : base(title, keyHelpText)
        {
            AddInternal(waitingText = new SpriteText
            {
                Text = @"Waiting for target selection...",
                Font = FrameworkFont.Regular,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });

            AddButton(@"choose target", () => ChooseTarget?.Invoke());
            AddButton(@"up one parent", () => GoUpOneParent?.Invoke());
            AddButton(@"toggle inspector", () => ToggleInspector?.Invoke());
        }

        protected override void Update()
        {
            waitingText.Alpha = visualiser.Searching ? 1 : 0;
            base.Update();
        }

        protected override bool OnClick(ClickEvent e) => true;
    }
}
