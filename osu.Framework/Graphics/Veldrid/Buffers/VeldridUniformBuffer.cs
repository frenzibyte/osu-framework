// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal interface IVeldridUniformBuffer : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="ResourceSet"/> containing the buffer attached to the given layout.
        ResourceSet GetResourceSet(ResourceLayout layout);

        IReadOnlyList<IVeldridUniformBufferStorage> Storages { get; }

        void FreeStorage(IVeldridUniformBufferStorage storage);

        /// <summary>
        /// Resets this <see cref="IVeldridUniformBuffer"/>, bringing it to the initial state.
        /// </summary>
        void ResetCounters();
    }

    internal class VeldridUniformBuffer<TData> : IUniformBuffer<TData>, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        private VeldridUniformBufferStorage<TData> currentStorage => storages[currentStorageIndex];

        public IReadOnlyList<IVeldridUniformBufferStorage> Storages => storages;

        private readonly List<VeldridUniformBufferStorage<TData>> storages = new List<VeldridUniformBufferStorage<TData>>();
        private int currentStorageIndex;
        private TData? pendingData;

        private readonly VeldridRenderer renderer;

        public VeldridUniformBuffer(VeldridRenderer renderer)
        {
            this.renderer = renderer;
            storages.Add(new VeldridUniformBufferStorage<TData>(this.renderer));
        }

        public TData Data
        {
            get => pendingData ?? currentStorage.Data;
            set
            {
                if (value.Equals(Data))
                    return;

                // Immediately flush the current draw call, since we'll be changing the contents of this UBO.
                renderer.FlushCurrentBatch(FlushBatchSource.SetUniform);

                pendingData = value;
            }
        }

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            flushPendingData();

            // Logger.Log($"Get resource data from storage at index {currentStorageIndex}");
            return currentStorage.GetResourceSet(layout);
        }

        private void flushPendingData()
        {
            if (pendingData is not TData pending)
                return;

            // Advance the storage index with every new data. Each draw call effectively has a unique UBO.
            if (!currentStorage.CanUse && ++currentStorageIndex == storages.Count)
                storages.Add(new VeldridUniformBufferStorage<TData>(renderer));

            // Register this UBO to be reset in the next draw call, but only the first time it receives new data.
            // This prevents usages like the global UBO from being registered multiple times.
            if (currentStorageIndex == 1)
                renderer.RegisterUniformBufferForReset(this);

            // Upload the data.
            currentStorage.Data = pending;
            pendingData = null;
        }

        public void FreeStorage(IVeldridUniformBufferStorage storage)
        {
            storages.Remove((VeldridUniformBufferStorage<TData>)storage);
            storage.Dispose();
        }

        public void ResetCounters()
        {
            currentStorageIndex = 0;

            foreach (var s in storages)
                s.Reset();
        }

        public void Dispose()
        {
            foreach (var s in storages)
                s.Dispose();
        }
    }
}
