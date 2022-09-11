// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Development;
using osu.Framework.Extensions.ImageExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Lists;
using osu.Framework.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using PixelFormat = Veldrid.PixelFormat;
using Texture = Veldrid.Texture;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    internal class VeldridTexture : INativeTexture
    {
        /// <summary>
        /// Contains all currently-active <see cref="VeldridTexture"/>s.
        /// </summary>
        private static readonly LockedWeakList<VeldridTexture> all_textures = new LockedWeakList<VeldridTexture>();

        private readonly Queue<ITextureUpload> uploadQueue = new Queue<ITextureUpload>();

        IRenderer INativeTexture.Renderer => renderer;

        public string Identifier
        {
            get
            {
                if (!Available || texture == null)
                    return "-";

                return texture.Name;
            }
        }

        public int MaxSize => renderer.MaxTextureSize;

        public virtual int Width { get; set; }
        public virtual int Height { get; set; }
        public virtual int GetByteSize() => Width * Height * 4;
        public bool Available { get; private set; } = true;
        public bool BypassTextureUploadQueueing { get; set; }

        private readonly bool manualMipmaps;

        private readonly SamplerFilter filteringMode;
        private readonly Rgba32 initialisationColour;

        public ulong BindCount { get; protected set; }

        /// <summary>
        /// Invoked when a new <see cref="VeldridTexture"/> is created.
        /// </summary>
        /// <remarks>
        /// Invocation from the draw or update thread cannot be assumed.
        /// </remarks>
        public static event Action<VeldridTexture> TextureCreated;

        public RectangleI Bounds => new RectangleI(0, 0, Width, Height);

        protected virtual TextureUsage Usages
        {
            get
            {
                var usages = TextureUsage.Sampled;

                if (!manualMipmaps)
                    usages |= TextureUsage.GenerateMipmaps;

                return usages;
            }
        }

        private readonly VeldridRenderer renderer;

        /// <summary>
        /// Creates a new <see cref="VeldridTexture"/>.
        /// </summary>
        /// <param name="renderer">The renderer.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="manualMipmaps">Whether manual mipmaps will be uploaded to the texture. If false, the texture will compute mipmaps automatically.</param>
        /// <param name="filteringMode">The filtering mode.</param>
        /// <param name="initialisationColour">The colour to initialise texture levels with (in the case of sub region initial uploads).</param>
        public VeldridTexture(VeldridRenderer renderer, int width, int height, bool manualMipmaps = false, SamplerFilter filteringMode = SamplerFilter.MinLinear_MagLinear_MipLinear, Rgba32 initialisationColour = default)
        {
            this.renderer = renderer;
            this.manualMipmaps = manualMipmaps;
            this.filteringMode = filteringMode;
            this.initialisationColour = initialisationColour;

            Width = width;
            Height = height;

            all_textures.Add(this);

            TextureCreated?.Invoke(this);
        }

        /// <summary>
        /// Retrieves all currently-active <see cref="VeldridTexture"/>s.
        /// </summary>
        public static VeldridTexture[] GetAllTextures() => all_textures.ToArray();

        #region Disposal

        ~VeldridTexture()
        {
            Dispose(false);
        }

        private bool isDisposed;

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool isDisposing)
        {
            all_textures.Remove(this);

            while (tryGetNextUpload(out var upload))
                upload.Dispose();

            renderer.ScheduleDisposal(t =>
            {
                t.Available = false;

                if (t.texture == null)
                    return;

                t.memoryLease?.Dispose();

                t.resource?.Dispose();
                t.resource = null;

                t.sampler?.Dispose();
                t.sampler = null;

                t.texture?.Dispose();
                t.texture = null;
            }, this);
        }

        #endregion

        #region Memory Tracking

        private List<long> levelMemoryUsage = new List<long>();

        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        private void updateMemoryUsage(int level, long newUsage)
        {
            levelMemoryUsage ??= new List<long>();

            while (level >= levelMemoryUsage.Count)
                levelMemoryUsage.Add(0);

            levelMemoryUsage[level] = newUsage;

            memoryLease?.Dispose();
            memoryLease = NativeMemoryTracker.AddMemory(this, getMemoryUsage());
        }

        private long getMemoryUsage()
        {
            long usage = 0;

            for (int i = 0; i < levelMemoryUsage.Count; i++)
                usage += levelMemoryUsage[i];

            return usage;
        }

        #endregion

        private Texture texture;
        private Sampler sampler;

        private VeldridTextureSamplerSet resource;

        [CanBeNull]
        public VeldridTextureSamplerSet Resource
        {
            get
            {
                if (!Available)
                    throw new ObjectDisposedException(ToString(), "Can not obtain resource set of a disposed texture.");

                return resource;
            }
        }

        public void FlushUploads()
        {
            while (tryGetNextUpload(out var upload))
                upload.Dispose();
        }

        public void SetData(ITextureUpload upload)
        {
            lock (uploadQueue)
            {
                bool requireUpload = uploadQueue.Count == 0;
                uploadQueue.Enqueue(upload);

                if (requireUpload && !BypassTextureUploadQueueing)
                    renderer.EnqueueTextureUpload(this);
            }
        }

        public virtual bool Bind(int unit, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed texture.");

            Upload();

            if (resource == null)
                return false;

            if (renderer.BindTexture(this, wrapModeS: wrapModeS, wrapModeT: wrapModeT))
                BindCount++;

            return true;
        }

        public bool Upload()
        {
            if (!Available)
                return false;

            // We should never run raw Veldrid calls on another thread than the draw thread due to race conditions.
            ThreadSafety.EnsureDrawThread();

            bool didUpload = false;

            while (tryGetNextUpload(out ITextureUpload upload))
            {
                using (upload)
                {
                    DoUpload(upload);
                    didUpload = true;
                }
            }

            if (didUpload && !(manualMipmaps || maximumUploadedLod > 0))
                renderer.Commands.GenerateMipmaps(texture);

            return didUpload;
        }

        public bool UploadComplete
        {
            get
            {
                lock (uploadQueue)
                    return uploadQueue.Count == 0;
            }
        }

        /// <summary>
        /// Whether the texture is currently queued for upload.
        /// </summary>
        public bool IsQueuedForUpload { get; set; }

        private bool tryGetNextUpload(out ITextureUpload upload)
        {
            lock (uploadQueue)
            {
                if (uploadQueue.Count == 0)
                {
                    upload = null;
                    return false;
                }

                upload = uploadQueue.Dequeue();
                return true;
            }
        }

        /// <summary>
        /// The maximum number of mip levels provided by a <see cref="ITextureUpload"/>.
        /// </summary>
        /// <remarks>
        /// This excludes automatic generation of mipmaps via the graphics backend.
        /// </remarks>
        private int maximumUploadedLod;

        protected virtual void DoUpload(ITextureUpload upload)
        {
            if (texture == null || texture.Width != Width || texture.Height != Height)
            {
                resource?.Dispose();
                resource = null;

                sampler?.Dispose();
                sampler = null;

                texture?.Dispose();

                var textureDescription = TextureDescription.Texture2D((uint)Width, (uint)Height, (uint)calculateMipmapLevels(Width, Height), 1, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, Usages);
                texture = renderer.Factory.CreateTexture(textureDescription);

                // todo: we may want to look into not having to allocate chunks of zero byte region for initialising textures
                // similar to how OpenGL allows calling glTexImage2D with null data pointer.
                initialiseLevel(0, Width, Height);

                maximumUploadedLod = 0;
            }

            int lastMaximumUploadedLod = maximumUploadedLod;

            if (!upload.Data.IsEmpty)
            {
                // ensure all mip levels up to the target level are initialised.
                if (upload.Level > maximumUploadedLod)
                {
                    for (int i = maximumUploadedLod + 1; i <= upload.Level; i++)
                        initialiseLevel(i, Width >> i, Height >> i);

                    maximumUploadedLod = upload.Level;
                }

                renderer.UpdateTexture(texture, upload.Bounds.X >> upload.Level, upload.Bounds.Y >> upload.Level, upload.Bounds.Width >> upload.Level, upload.Bounds.Height >> upload.Level, upload.Level, upload.Data);
            }

            if (sampler == null || maximumUploadedLod > lastMaximumUploadedLod)
            {
                resource?.Dispose();
                resource = null;

                sampler?.Dispose();

                bool useUploadMipmaps = manualMipmaps || maximumUploadedLod > 0;

                var samplerDescription = new SamplerDescription
                {
                    AddressModeU = SamplerAddressMode.Clamp,
                    AddressModeV = SamplerAddressMode.Clamp,
                    AddressModeW = SamplerAddressMode.Clamp,
                    Filter = filteringMode,
                    MinimumLod = 0,
                    MaximumLod = useUploadMipmaps ? (uint)maximumUploadedLod : IRenderer.MAX_MIPMAP_LEVELS,
                    MaximumAnisotropy = 0,
                };

                sampler = renderer.Factory.CreateSampler(samplerDescription);
            }

            resource ??= new VeldridTextureSamplerSet(renderer, texture, sampler);
        }

        private unsafe void initialiseLevel(int level, int width, int height)
        {
            using (var image = createBackingImage(width, height))
            using (var pixels = image.CreateReadOnlyPixelSpan())
            {
                updateMemoryUsage(level, (long)width * height * sizeof(Rgba32));
                renderer.UpdateTexture(texture, 0, 0, width, height, level, pixels.Span);
            }
        }

        private Image<Rgba32> createBackingImage(int width, int height)
        {
            // it is faster to initialise without a background specification if transparent black is all that's required.
            return initialisationColour == default
                ? new Image<Rgba32>(width, height)
                : new Image<Rgba32>(width, height, initialisationColour);
        }

        // todo: should this be limited to MAX_MIPMAP_LEVELS or was that constant supposed to be for automatic mipmap generation only?
        // previous implementation was allocating mip levels all the way to 1x1 size when an ITextureUpload.Level > 0, therefore it's not limited there.
        private static int calculateMipmapLevels(int width, int height) => 1 + (int)Math.Floor(Math.Log(Math.Max(width, height), 2));
    }
}
