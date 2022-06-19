// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Graphics.Visualisation
{
    internal class InputInspector : VisualisationInspector
    {
        private GridContainer content = null!;
        private TabControl<InputQueueType> inspectorTabControl = null!;
        private Container tabContentContainer = null!;

        private InputQueueVisualiser positionalVisualiser = null!;
        private InputQueueVisualiser nonPositionalVisualiser = null!;

        /// <summary>
        /// The drawable which was initially selected upon choosing target.
        /// Unlike <see cref="VisualisationInspector.InspectedDrawable"/>, which can only contain valid drawables.
        /// </summary>
        public readonly Bindable<Drawable> SelectedDrawable = new Bindable<Drawable>();

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = content = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension()
                },
                ColumnDimensions = new[] { new Dimension() },
                Content = new[]
                {
                    new Drawable[]
                    {
                        inspectorTabControl = new BasicTabControl<InputQueueType>
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 20,
                            Margin = new MarginPadding
                            {
                                Horizontal = 10,
                                Vertical = 5
                            },
                            Items = Enum.GetValues(typeof(InputQueueType)).Cast<InputQueueType>().ToList()
                        },
                        tabContentContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                positionalVisualiser = new InputQueueVisualiser(InputQueueType.Positional)
                                {
                                    SelectedDrawable = { BindTarget = SelectedDrawable },
                                    InspectedInput = { BindTarget = InspectedDrawable },
                                },
                                nonPositionalVisualiser = new InputQueueVisualiser(InputQueueType.NonPositional)
                                {
                                    SelectedDrawable = { BindTarget = SelectedDrawable },
                                    InspectedInput = { BindTarget = InspectedDrawable },
                                },
                            }
                        }
                    }
                }.Invert()
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            inspectorTabControl.Current.BindValueChanged(showTab, true);
        }

        private void showTab(ValueChangedEvent<InputQueueType> tabChanged)
        {
            tabContentContainer.Children.ForEach(tab => tab.Hide());

            switch (tabChanged.NewValue)
            {
                case InputQueueType.Positional:
                    positionalVisualiser.Show();
                    break;

                case InputQueueType.NonPositional:
                    nonPositionalVisualiser.Show();
                    break;
            }
        }

        protected override void PopIn()
        {
            base.PopIn();
            content.Show();
        }

        protected override void PopOut()
        {
            base.PopOut();
            content.Hide();
        }
    }
}
