// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.IO.Stores;

namespace osu.Framework.Audio.Sample
{
    internal class SampleStore : AudioCollectionManager<AdjustableAudioComponent>, ISampleStore
    {
        private readonly IResourceStore<byte[]> store;

        public int PlaybackConcurrency { get; set; } = Sample.DEFAULT_CONCURRENCY;

        internal SampleStore([NotNull] IResourceStore<byte[]> store)
        {
            this.store = store;

            (store as ResourceStore<byte[]>)?.AddExtension(@"wav");
            (store as ResourceStore<byte[]>)?.AddExtension(@"mp3");
        }

        public Sample Get(string name)
        {
            if (IsDisposed) throw new ObjectDisposedException($"Cannot retrieve items for an already disposed {nameof(SampleStore)}");

            if (string.IsNullOrEmpty(name)) return null;

            var sample = new SampleVirtual();
            AddItem(sample);
            return sample;
        }

        public Task<Sample> GetAsync(string name) => Task.Run(() => Get(name));

        public Stream GetStream(string name) => store.GetStream(name);

        public IEnumerable<string> GetAvailableResources() => store.GetAvailableResources();
    }
}
