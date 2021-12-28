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
    /// A staging <see cref="DeviceBuffer"/> pool for temporary consumption.
    /// </summary>
    internal static class VeldridStagingBufferPool
    {
        private static readonly List<DeviceBuffer> available_buffers = new List<DeviceBuffer>();
        private static readonly List<DeviceBuffer> used_buffers = new List<DeviceBuffer>();

        private static readonly GlobalStatistic<int> stat_available_count = GlobalStatistics.Get<int>("Veldrid pools", "Available staging buffers");
        private static readonly GlobalStatistic<int> stat_used_count = GlobalStatistics.Get<int>("Veldrid pools", "Used staging buffers");

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
                    stat_available_count.Value--;
                    break;
                }
            }

            minimumSize = Math.Max(64, minimumSize);

            buffer ??= Vd.Factory.CreateBuffer(new BufferDescription((uint)minimumSize, BufferUsage.Staging));

            used_buffers.Add(buffer);
            stat_used_count.Value++;
            return buffer;
        }

        /// <summary>
        /// Releases all staging <see cref="DeviceBuffer"/>s and mark them back as available.
        /// </summary>
        public static void Release()
        {
            available_buffers.AddRange(used_buffers);
            stat_available_count.Value = available_buffers.Count;

            used_buffers.Clear();
            stat_used_count.Value = 0;
        }
    }
}
