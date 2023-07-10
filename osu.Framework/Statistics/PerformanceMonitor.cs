﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.ObjectPool;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Platform.MacOS.Native;
using osu.Framework.Utils;
using osu.Framework.Threading;
using osu.Framework.Timing;

namespace osu.Framework.Statistics
{
    internal class PerformanceMonitor : IDisposable
    {
        private readonly StopwatchClock ourClock = new StopwatchClock(true);

        private readonly Stack<PerformanceCollectionType> currentCollectionTypeStack = new Stack<PerformanceCollectionType>();

        private readonly InvokeOnDisposal[] endCollectionDelegates = new InvokeOnDisposal[FrameStatistics.NUM_PERFORMANCE_COLLECTION_TYPES];

        private BackgroundStackTraceCollector traceCollector;

        private FrameStatistics currentFrame;

        private const int max_pending_frames = 10;

        private readonly string threadName;

        internal readonly ConcurrentQueue<FrameStatistics> PendingFrames = new ConcurrentQueue<FrameStatistics>();

        internal readonly ObjectPool<FrameStatistics> FramesPool =
            new DefaultObjectPoolProvider { MaximumRetained = max_pending_frames }
                .Create(new DefaultPooledObjectPolicy<FrameStatistics>());

        internal bool[] ActiveCounters { get; } = new bool[FrameStatistics.NUM_STATISTICS_COUNTER_TYPES];

        private bool enablePerformanceProfiling;

        public bool EnablePerformanceProfiling
        {
            set
            {
                enablePerformanceProfiling = value;
                updateEnabledState();
            }
        }

        private double consumptionTime;

        private readonly IBindable<bool> isActive;

        internal readonly ThrottledFrameClock Clock;

        private Thread thread;

        public double FrameAimTime => 1000.0 / (Clock?.MaximumUpdateHz > 0 ? Clock.MaximumUpdateHz : double.MaxValue);

        public readonly OSLog AppleLog;
        public readonly OSSignpostID AppleSignpost;

        internal PerformanceMonitor(GameThread thread, IEnumerable<StatisticsCounterType> counters)
        {
            Clock = thread.Clock;
            threadName = thread.Name;

            isActive = thread.IsActive.GetBoundCopy();
            isActive.BindValueChanged(_ => updateEnabledState());

            currentFrame = FramesPool.Get();

            foreach (var c in counters)
                ActiveCounters[(int)c] = true;

            for (int i = 0; i < FrameStatistics.NUM_PERFORMANCE_COLLECTION_TYPES; i++)
            {
                var t = (PerformanceCollectionType)i;
                endCollectionDelegates[i] = new InvokeOnDisposal(() => endCollecting(t, t >= PerformanceCollectionType.DrawCall));
            }

            if (RuntimeInfo.IsApple && thread is DrawThread)
            {
                AppleLog = OS.os_log_create(threadName, OS.OS_LOG_CATEGORY_POINTS_OF_INTEREST);
                AppleSignpost = OS.os_signpost_id_generate(AppleLog);
            }
        }

        /// <summary>
        /// Switch target thread to <see cref="Thread.CurrentThread"/>.
        /// </summary>
        public void MakeCurrent()
        {
            var current = Thread.CurrentThread;

            if (current == thread)
                return;

            thread = Thread.CurrentThread;

            traceCollector?.Dispose();
            traceCollector = new BackgroundStackTraceCollector(thread, ourClock, threadName);
            updateEnabledState();
        }

        /// <summary>
        /// Start collecting a type of passing time.
        /// </summary>
        public InvokeOnDisposal BeginCollecting(PerformanceCollectionType type, bool signpost = false)
        {
            // Consume time, regardless of whether we are using it at this point.
            // If not, an `EndCollecting` call may end up reporting more time than actually passed between
            // the Begin-End pair.
            double time = consumeStopwatchElapsedTime();

            if (currentCollectionTypeStack.Count > 0)
            {
                PerformanceCollectionType t = currentCollectionTypeStack.Peek();

                currentFrame.CollectedTimes.TryAdd(t, 0);
                currentFrame.CollectedTimes[t] += time;
            }

            currentCollectionTypeStack.Push(type);

            // if (RuntimeInfo.IsApple && AppleLog.Handle != IntPtr.Zero && signpost)
            //     OS.signpost_interval_begin(AppleLog, AppleSignpost, Enum.GetName(type)!);

            return endCollectionDelegates[(int)type];
        }

        /// <summary>
        /// End collecting a type of passing time (that was previously started).
        /// </summary>
        /// <param name="type"></param>
        private void endCollecting(PerformanceCollectionType type, bool signpost = false)
        {
            // if (RuntimeInfo.IsApple && AppleLog.Handle != IntPtr.Zero && signpost)
            //     OS.signpost_interval_end(AppleLog, AppleSignpost, Enum.GetName(type)!);

            currentCollectionTypeStack.Pop();

            currentFrame.CollectedTimes.TryAdd(type, 0);
            currentFrame.CollectedTimes[type] += consumeStopwatchElapsedTime();
        }

        private readonly int[] lastAmountGarbageCollects = new int[3];

        public bool HandleGC;

        private readonly Dictionary<StatisticsCounterType, GlobalStatistic<long>> globalStatistics = new Dictionary<StatisticsCounterType, GlobalStatistic<long>>();

        /// <summary>
        /// Resets all frame statistics. Run exactly once per frame.
        /// </summary>
        public void NewFrame()
        {
            // Reset the counters we keep track of
            for (int i = 0; i < ActiveCounters.Length; ++i)
            {
                if (ActiveCounters[i])
                {
                    long count = FrameStatistics.COUNTERS[i];
                    var type = (StatisticsCounterType)i;

                    if (!globalStatistics.TryGetValue(type, out var global))
                        globalStatistics[type] = global = GlobalStatistics.Get<long>(threadName, type.ToString());

                    global.Value = count;
                    currentFrame.Counts[type] = count;
                    currentFrame.FramesPerSecond = Clock.FramesPerSecond;
                    currentFrame.Jitter = Clock.Jitter;

                    FrameStatistics.COUNTERS[i] = 0;
                }
            }

            if (PendingFrames.Count < max_pending_frames - 1)
            {
                PendingFrames.Enqueue(currentFrame);
                currentFrame = FramesPool.Get();
            }

            currentFrame.Clear();

            if (HandleGC)
            {
                for (int i = 0; i < lastAmountGarbageCollects.Length; ++i)
                {
                    int amountCollections = GC.CollectionCount(i);

                    if (lastAmountGarbageCollects[i] != amountCollections)
                    {
                        lastAmountGarbageCollects[i] = amountCollections;
                        currentFrame.GarbageCollections.Add(i);
                    }
                }
            }

            double dampRate = Math.Max(Clock.ElapsedFrameTime, 0) / 1000;
            averageFrameTime = Interpolation.Damp(averageFrameTime, Clock.ElapsedFrameTime, 0.01, dampRate);

            //check for dropped (stutter) frames
            traceCollector?.NewFrame(Clock.ElapsedFrameTime, Math.Max(10, Math.Max(1000 / Clock.MaximumUpdateHz, averageFrameTime) * 4));

            consumeStopwatchElapsedTime();
        }

        public void EndFrame()
        {
            traceCollector?.EndFrame();
        }

        private void updateEnabledState()
        {
            if (traceCollector != null)
                traceCollector.Enabled = enablePerformanceProfiling && isActive.Value;
        }

        private double averageFrameTime;

        private double consumeStopwatchElapsedTime()
        {
            double last = consumptionTime;

            consumptionTime = ourClock.CurrentTime;
            if (traceCollector != null)
                traceCollector.LastConsumptionTime = consumptionTime;

            return consumptionTime - last;
        }

        internal double FramesPerSecond => Clock.FramesPerSecond;

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                traceCollector?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public void BeginInterval(string name)
        {
            if (Renderer.StaticFrameIndex % 100 == 0)
                OS.signpost_interval_begin(AppleLog, AppleSignpost, name);
        }

        public void EndInterval(string name)
        {
            if (Renderer.StaticFrameIndex % 100 == 0)
                OS.signpost_interval_end(AppleLog, AppleSignpost, name);
        }
    }
}
