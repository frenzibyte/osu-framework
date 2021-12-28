// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Statistics;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;

namespace osu.Framework.Platform.SDL2
{
    internal abstract class VeldridPool<T>
        where T : class, IDisposable
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
        /// Releases all used resources and mark them as available.
        /// </summary>
        public void ReleaseAllUsedResources()
        {
            foreach (var used in UsedResources)
                AvailableResources.Add(used);

            statAvailableCount.Value = AvailableResources.Count;

            UsedResources.Clear();
            statUsedCount.Value = 0;
        }

        public void FreeUnusedResources()
        {
            AvailableResources.RemoveWhere(available =>
            {
                if (Vd.ResetId - available.useId > Vd.RESOURCES_FREE_CHECK_INTERVAL)
                {
                    available.resource.Dispose();
                    return true;
                }

                return false;
            });
        }
    }
}
