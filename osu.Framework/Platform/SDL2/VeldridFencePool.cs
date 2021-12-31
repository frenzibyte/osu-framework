// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;

namespace osu.Framework.Platform.SDL2
{
    internal class VeldridFencePool : VeldridPool<Fence>
    {
        /// <summary>
        /// The latest use ID of the used fences that have been signaled.
        /// </summary>
        public ulong LatestSignaledUseID
        {
            get
            {
                ulong latestUseID = 0;

                foreach (var used in UsedResources)
                {
                    if (used.resource.Signaled)
                        latestUseID = Math.Max(used.useId, latestUseID);
                }

                return latestUseID;
            }
        }

        public VeldridFencePool()
            : base("Synchronisation fences")
        {
        }

        /// <summary>
        /// Returns a synchronisation fence from the pool.
        /// </summary>
        public Fence Get()
        {
            var fence = Get(_ => true, () => Vd.Factory.CreateFence(false));
            fence.Reset();
            return fence;
        }
    }
}
