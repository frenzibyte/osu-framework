// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;
using Vd = osu.Framework.Graphics.Renderer.VeldridGraphicsBackend;

namespace osu.Framework.Graphics.Renderer.Pooling
{
    internal class RendererFencePool : RendererPool<Fence>
    {
        /// <summary>
        /// The latest use ID of the used fences that have been signaled.
        /// </summary>
        public ulong LatestSignaledUseID
        {
            get
            {
                ulong id = 0;

                foreach (var used in UsedResources)
                {
                    if (used.resource.Signaled)
                        id = Math.Max(used.useId, id);
                }

                return id;
            }
        }

        public RendererFencePool()
            : base("Synchronisation fences")
        {
        }

        protected override bool CanReuseResource(Fence fence)
        {
            fence.Reset();
            return true;
        }

        protected override Fence CreateResource() => Vd.Factory.CreateFence(false);
    }
}
