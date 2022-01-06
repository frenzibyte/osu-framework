// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid;

namespace osu.Framework.Graphics.Rendering.Pooling
{
    internal class RendererFencePool : RendererPool<Fence>
    {
        /// <summary>
        /// The latest use ID of the used fences that have been signaled.
        /// </summary>
        public ulong? LatestSignaledUseID
        {
            get
            {
                for (var node = UsedResources.Last; node != null; node = node.Previous)
                {
                    if (node.Value.resource.Signaled)
                        return node.Value.useId;
                }

                return null;
            }
        }

        public RendererFencePool()
            : base("Synchronisation fences")
        {
        }

        protected override bool CanUseResource(Fence fence)
        {
            fence.Reset();
            return true;
        }

        protected override Fence CreateResource() => Renderer.Factory.CreateFence(false);
    }
}
