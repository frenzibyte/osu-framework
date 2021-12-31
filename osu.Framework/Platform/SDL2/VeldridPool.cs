// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Statistics;
using Veldrid;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;

namespace osu.Framework.Platform.SDL2
{
    /// <summary>
    /// A pool managing over device resources, designed to handle GPU-side memory access.
    /// </summary>
    /// <typeparam name="T">The device resource type.</typeparam>
    internal abstract class VeldridPool<T>
        where T : class, DeviceResource, IDisposable
    {
        protected readonly HashSet<(ulong useId, T resource)> AvailableResources = new HashSet<(ulong, T)>();
        protected readonly HashSet<(ulong useId, T resource)> UsedResources = new HashSet<(ulong, T)>();

        private readonly GlobalStatistic<int> statAvailableCount;
        private readonly GlobalStatistic<int> statUsedCount;

        protected VeldridPool(string name)
        {
            statAvailableCount = GlobalStatistics.Get<int>("Veldrid pools", $"Available {name.ToLower()}");
            statUsedCount = GlobalStatistics.Get<int>("Veldrid pools", $"Used {name.ToLower()}");
        }

        protected T Get(Predicate<T> match, Func<T> create)
        {
            T resource = null;

            foreach (var available in AvailableResources)
            {
                if (match(available.resource))
                {
                    resource = available.resource;
                    AvailableResources.Remove(available);
                    statAvailableCount.Value--;
                    break;
                }
            }

            resource ??= create();

            UsedResources.Add((Vd.ResetId, resource));
            statUsedCount.Value++;
            return resource;
        }

        /// <summary>
        /// Releases all resources that were marked as used up the specified use ID, and mark them as available.
        /// </summary>
        /// <param name="untilId">The latest use ID in which used resources can be released.</param>
        public void ReleaseUsedResources(ulong untilId)
        {
            UsedResources.RemoveWhere(used =>
            {
                if (used.useId <= untilId)
                {
                    AvailableResources.Add(used);
                    return true;
                }

                return false;
            });

            statAvailableCount.Value = AvailableResources.Count;
            statUsedCount.Value = UsedResources.Count;
        }

        /// <summary>
        /// Frees all resources that were left unused for a specified frame interval.
        /// </summary>
        /// <param name="resourceFreeInterval">The frame interval to free the resource.</param>
        public void FreeUnusedResources(ulong resourceFreeInterval)
        {
            AvailableResources.RemoveWhere(available =>
            {
                if (Vd.ResetId - available.useId > resourceFreeInterval)
                {
                    available.resource.Dispose();
                    return true;
                }

                return false;
            });

            statAvailableCount.Value = AvailableResources.Count;
        }
    }
}
