// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;

namespace osu.Framework.Platform.SDL2
{
    /// <summary>
    /// A staging <see cref="DeviceBuffer"/> pool for temporary consumption.
    /// </summary>
    internal class VeldridStagingBufferPool : VeldridPool<DeviceBuffer>
    {
        public VeldridStagingBufferPool()
            : base("Staging Buffers")
        {
        }

        /// <summary>
        /// Returns a staging <see cref="DeviceBuffer"/> from the pool with a specified minimum size.
        /// </summary>
        /// <param name="minimumSize">The minimum size of the returned buffer.</param>
        public DeviceBuffer Get(int minimumSize)
        {
            minimumSize = Math.Max(64, minimumSize);
            return Get(b => b.SizeInBytes >= minimumSize, () => Vd.Factory.CreateBuffer(new BufferDescription((uint)minimumSize, BufferUsage.Staging)));
        }
    }
}
