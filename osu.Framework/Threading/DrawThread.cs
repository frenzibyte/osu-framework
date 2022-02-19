// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Development;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Platform;

namespace osu.Framework.Threading
{
    public class DrawThread : GameThread
    {
        private readonly GameHost host;

        public DrawThread(Action onNewFrame, GameHost host)
            : base(onNewFrame, "Draw")
        {
            this.host = host;
        }

        public override bool IsCurrent => ThreadSafety.IsDrawThread;

        protected sealed override void OnInitialize()
        {
            var window = host.Window;

            if (window != null)
            {
                // todo: this can't exist.
                // I have not the slightest idea why calling Vd.Initialise(host) directly doesn't work.
                // calling it with Task.Factory.StartNew or from main/input thread works just fine on the other hand....
                Task.Factory.StartNew(() => Vd.Initialise(host), TaskCreationOptions.LongRunning).WaitSafely();
            }
        }

        internal sealed override void MakeCurrent()
        {
            base.MakeCurrent();

            ThreadSafety.IsDrawThread = true;
        }

        internal override IEnumerable<StatisticsCounterType> StatisticsCounters => new[]
        {
            StatisticsCounterType.VBufBinds,
            StatisticsCounterType.VBufOverflow,
            StatisticsCounterType.TextureBinds,
            StatisticsCounterType.FBORedraw,
            StatisticsCounterType.DrawCalls,
            StatisticsCounterType.ShaderBinds,
            StatisticsCounterType.VerticesDraw,
            StatisticsCounterType.VerticesUpl,
            StatisticsCounterType.Pixels,
        };
    }
}
