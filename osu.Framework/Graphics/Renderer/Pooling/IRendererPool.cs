// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Renderer.Pooling
{
    /// <summary>
    /// A pool managing over device resources, designed to handle GPU-side memory access.
    /// </summary>
    internal interface IRendererPool
    {
        /// <summary>
        /// Releases all resources that were marked as used up the specified use ID, and mark them as available.
        /// </summary>
        /// <param name="untilId">The latest use ID in which used resources can be released.</param>
        void ReleaseUsedResources(ulong untilId);

        /// <summary>
        /// Frees all resources that were left unused for a specified frame interval.
        /// </summary>
        /// <param name="resourceFreeInterval">The frame interval to free the resource.</param>
        /// <returns>Whether any unused resource has been freed.</returns>
        bool FreeUnusedResources(ulong resourceFreeInterval);
    }
}
