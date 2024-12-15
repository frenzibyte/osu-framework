// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Handlers.Pen
{
    /// <summary>
    /// Handles pen events from an <see cref="ISDLWindow"/>.
    /// This outputs simple mouse input with <see cref="ISourcedFromPen"/> markers embedded.
    /// </summary>
    public class PenHandler : InputHandler
    {
        private static readonly GlobalStatistic<ulong> statistic_total_events = GlobalStatistics.Get<ulong>(StatisticGroupFor<PenHandler>(), "Total events");

        public override string Description => "Pen";

        public override bool IsActive => true;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (!(host.Window is ISDLWindow sdlWindow))
                return false;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    sdlWindow.PenIn += handlePenIn;
                    sdlWindow.PenOut += handlePenOut;
                    sdlWindow.PenMove += handlePenMove;
                    sdlWindow.PenDown += handlePenDown;
                    sdlWindow.PenUp += handlePenUp;
                }
                else
                {
                    sdlWindow.PenIn -= handlePenIn;
                    sdlWindow.PenOut -= handlePenOut;
                    sdlWindow.PenMove -= handlePenMove;
                    sdlWindow.PenDown -= handlePenDown;
                    sdlWindow.PenUp -= handlePenUp;
                }
            }, true);

            return true;
        }

        private void handlePenIn()
        {
            // The first pen motion will validate the mouse position, we don't have to do anything here.
        }

        private void handlePenOut() => enqueueInput(new MouseInvalidatePositionInputFromPen());

        private void handlePenMove(Vector2 position) => enqueueInput(new MousePositionAbsoluteInputFromPen { Position = position });

        private void handlePenDown(MouseButton button) => enqueueInput(new MouseButtonInputFromPen(button, true));

        private void handlePenUp(MouseButton button) => enqueueInput(new MouseButtonInputFromPen(button, false));

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
            statistic_total_events.Value++;
        }
    }
}
