// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Veldrid;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;

namespace osu.Framework.Platform.SDL2
{
    /// <summary>
    /// A staging <see cref="DeviceBuffer"/> pool for temporary consumption.
    /// </summary>
    internal static class StagingBufferPool
    {
        private static readonly List<DeviceBuffer> available_buffers = new List<DeviceBuffer>();
        private static readonly List<DeviceBuffer> used_buffers = new List<DeviceBuffer>();

        /// <summary>
        /// Returns a staging <see cref="DeviceBuffer"/> from the pool with a specified minimum size.
        /// </summary>
        /// <param name="minimumSize">The minimum size of the returned buffer.</param>
        public static DeviceBuffer Get(int minimumSize)
        {
            DeviceBuffer buffer = null;

            foreach (DeviceBuffer b in available_buffers)
            {
                if (b.SizeInBytes >= minimumSize)
                {
                    buffer = b;
                    available_buffers.Remove(b);
                    break;
                }
            }

            buffer ??= Vd.Factory.CreateBuffer(new BufferDescription((uint)minimumSize, BufferUsage.Staging));
            used_buffers.Add(buffer);
            return buffer;
        }

        /// <summary>
        /// Releases all staging <see cref="DeviceBuffer"/>s and mark them back as available.
        /// </summary>
        public static void Release()
        {
            available_buffers.AddRange(used_buffers);
            used_buffers.Clear();
        }
    }
}
