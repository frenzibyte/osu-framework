// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;
using osu.Framework.Graphics.Rendering.Textures;
using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture store that bypasses atlasing and removes textures from memory after dereferenced by all consumers.
    /// </summary>
    public class LargeTextureStore : TextureStore
    {
        private readonly object referenceCountLock = new object();
        private readonly Dictionary<string, TextureWithRefCount.ReferenceCount> referenceCounts = new Dictionary<string, TextureWithRefCount.ReferenceCount>();

        public LargeTextureStore(IResourceStore<TextureUpload> store = null, FilteringMode filteringMode = FilteringMode.Linear)
            : base(store, false, filteringMode, true)
        {
        }

        protected override bool TryGetCached(string lookupKey, out Texture texture)
        {
            lock (referenceCountLock)
            {
                if (base.TryGetCached(lookupKey, out var tex))
                {
                    texture = createTextureWithRefCount(lookupKey, tex);
                    return true;
                }

                texture = null;
                return false;
            }
        }

        protected override Texture CacheAndReturnTexture(string lookupKey, Texture texture)
        {
            lock (referenceCountLock)
                return createTextureWithRefCount(lookupKey, base.CacheAndReturnTexture(lookupKey, texture));
        }

        private TextureWithRefCount createTextureWithRefCount([NotNull] string lookupKey, [CanBeNull] Texture baseTexture)
        {
            if (baseTexture == null)
                return null;

            lock (referenceCountLock)
            {
                if (!referenceCounts.TryGetValue(lookupKey, out TextureWithRefCount.ReferenceCount count))
                    referenceCounts[lookupKey] = count = new TextureWithRefCount.ReferenceCount(referenceCountLock, () => onAllReferencesLost(baseTexture));

                return new TextureWithRefCount(baseTexture.RendererTexture, count);
            }
        }

        private void onAllReferencesLost(Texture texture)
        {
            Debug.Assert(Monitor.IsEntered(referenceCountLock));

            referenceCounts.Remove(texture.LookupKey);
            Purge(texture);
        }
    }
}
