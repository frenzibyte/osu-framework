// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Logging;
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

        private readonly int sizeInBytes;

        private int index = -4;

        public ulong LastUseResetId { get; private set; } = ulong.MaxValue;

        private ResourceSet? set;
        private TData data;

        public VeldridUniformBufferStorage(VeldridRenderer renderer)
        {
            this.renderer = renderer;

            sizeInBytes = Marshal.SizeOf(default(TData));

            buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)sizeInBytes * 100, BufferUsage.UniformBuffer));
            memoryLease = NativeMemoryTracker.AddMemory(this, buffer.SizeInBytes);

            IVeldridUniformBufferStorage.StorageCount.Value++;
        }

        public TData Data
        {
            get => data;
            set
            {
                data = value;

                index += 4;
                // Logger.Log($"Update data at offset = {index * sizeInBytes} ({index})");
                renderer.BufferUpdateCommands.UpdateBuffer(buffer, (uint)(index * sizeInBytes), ref data);
            }
        }

        public bool CanUse => index < 96;

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            if (LastUseResetId == renderer.ResetId)
                Debugger.Break();

            LastUseResetId = renderer.ResetId;

            // Logger.Log($"Get resource set at offset = {index * sizeInBytes} ({index})");
            return set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, new DeviceBufferRange(buffer, (uint)(index * (uint)sizeInBytes), (uint)sizeInBytes)));
        }

        public void Reset()
        {
            // Logger.Log("Storage is reset");
            index = -4;
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
