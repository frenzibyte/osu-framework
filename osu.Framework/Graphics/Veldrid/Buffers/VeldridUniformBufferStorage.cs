// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal interface IVeldridUniformBufferStorage : IDisposable
    {
        protected static GlobalStatistic<int> StorageCount { get; } = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Total UBO storages created");

        public ulong LastUseResetId { get; }
    }

    internal class VeldridUniformBufferStorage<TData> : IVeldridUniformBufferStorage
        where TData : unmanaged, IEquatable<TData>
    {
        private readonly VeldridRenderer renderer;
        private readonly DeviceBuffer buffer;
        private readonly NativeMemoryTracker.NativeMemoryLease memoryLease;

        public ulong LastUseResetId { get; private set; }

        private ResourceSet? set;
        private TData data;

        public VeldridUniformBufferStorage(VeldridRenderer renderer)
        {
            this.renderer = renderer;

            buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(default(TData)), BufferUsage.UniformBuffer));
            memoryLease = NativeMemoryTracker.AddMemory(this, buffer.SizeInBytes);

            IVeldridUniformBufferStorage.StorageCount.Value++;
        }

        public TData Data
        {
            get => data;
            set
            {
                data = value;
                renderer.BufferUpdateCommands.UpdateBuffer(buffer, 0, ref data);
            }
        }

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            LastUseResetId = renderer.ResetId;
            return set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffer));
        }

        public void Dispose()
        {
            buffer.Dispose();
            memoryLease.Dispose();
            set?.Dispose();

            IVeldridUniformBufferStorage.StorageCount.Value--;
        }
    }
}
