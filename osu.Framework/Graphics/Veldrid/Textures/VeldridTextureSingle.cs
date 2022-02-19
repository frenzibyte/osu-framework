// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Development;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Vertices;
using osu.Framework.Lists;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using Texture = Veldrid.Texture;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    internal class VeldridTextureSingle : VeldridTexture
    {
        /// <summary>
        /// Contains all currently-active <see cref="VeldridTextureSingle"/>es.
        /// </summary>
        private static readonly LockedWeakList<VeldridTextureSingle> all_textures = new LockedWeakList<VeldridTextureSingle>();

        public const int MAX_MIPMAP_LEVELS = 3;

        private static readonly Action<TexturedVertex2D> default_quad_action = new QuadBatch<TexturedVertex2D>(100, 1000).AddAction;

        private readonly Queue<ITextureUpload> uploadQueue = new Queue<ITextureUpload>();

        /// <summary>
        /// Invoked when a new <see cref="VeldridTextureAtlas"/> is created.
        /// </summary>
        /// <remarks>
        /// Invocation from the draw or update thread cannot be assumed.
        /// </remarks>
        public static event Action<VeldridTextureSingle> TextureCreated;

        private readonly FilteringMode filteringMode;

        private readonly Rgba32 initialisationColour;

        /// <summary>
        /// The total amount of times this <see cref="VeldridTextureSingle"/> was bound.
        /// </summary>
        public ulong BindCount { get; protected set; }

        // ReSharper disable once InconsistentlySynchronizedField (no need to lock here. we don't really care if the value is stale).
        public override bool Loaded => texture != null || uploadQueue.Count > 0;

        public override RectangleI Bounds => new RectangleI(0, 0, Width, Height);

        /// <summary>
        /// Creates a new <see cref="VeldridTextureSingle"/>.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="manualMipmaps">Whether manual mipmaps will be uploaded to the texture. If false, the texture will compute mipmaps automatically.</param>
        /// <param name="filteringMode">The filtering mode.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <param name="initialisationColour">The colour to initialise texture levels with (in the case of sub region initial uploads).</param>
        public VeldridTextureSingle(int width, int height, bool manualMipmaps = false, FilteringMode filteringMode = FilteringMode.Linear,
                                     WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None, Rgba32 initialisationColour = default)
            : base(wrapModeS, wrapModeT)
        {
            Width = width;
            Height = height;
            this.manualMipmaps = manualMipmaps;
            this.filteringMode = filteringMode;
            this.initialisationColour = initialisationColour;

            all_textures.Add(this);

            TextureCreated?.Invoke(this);
        }

        /// <summary>
        /// Retrieves all currently-active <see cref="VeldridTextureSingle"/>s.
        /// </summary>
        public static VeldridTextureSingle[] GetAllTextures() => all_textures.ToArray();

        #region Disposal

        ~VeldridTextureSingle()
        {
            Dispose(false);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            all_textures.Remove(this);

            while (tryGetNextUpload(out var upload))
                upload.Dispose();

            Vd.ScheduleDisposal(t =>
            {
                if (t.texture == null)
                    return;

                t.memoryLease?.Dispose();

                t.textureResourceSet?.Dispose();
                t.textureResourceSet = null;

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

        private int height;

        public override VeldridTexture Native => this;

        public override int Height
        {
            get => height;
            set => height = value;
        }

        private int width;

        public override int Width
        {
            get => width;
            set => width = value;
        }

        private Texture texture;
        private Sampler sampler;

        private TextureResourceSet textureResourceSet;

        public override TextureResourceSet TextureResourceSet
        {
            get
            {
                if (!Available)
                    throw new ObjectDisposedException(ToString(), "Can not obtain resource set of a disposed texture.");

                if (textureResourceSet == null)
                    throw new InvalidOperationException("Can not obtain resource set of a texture before uploading it.");

                return textureResourceSet;
            }
        }

        /// <summary>
        /// Retrieves the size of this texture in bytes.
        /// </summary>
        public virtual int GetByteSize() => Width * Height * 4;

        private static void rotateVector(ref Vector2 toRotate, float sin, float cos)
        {
            float oldX = toRotate.X;
            toRotate.X = toRotate.X * cos - toRotate.Y * sin;
            toRotate.Y = oldX * sin + toRotate.Y * cos;
        }

        public override RectangleF GetTextureRect(RectangleF? textureRect)
        {
            RectangleF texRect = textureRect != null
                ? new RectangleF(textureRect.Value.X, textureRect.Value.Y, textureRect.Value.Width, textureRect.Value.Height)
                : new RectangleF(0, 0, Width, Height);

            texRect.X /= width;
            texRect.Y /= height;
            texRect.Width /= width;
            texRect.Height /= height;

            return texRect;
        }

        public const int VERTICES_PER_TRIANGLE = 4;

        internal override void DrawTriangle(Triangle vertexTriangle, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                            Vector2? inflationPercentage = null, RectangleF? textureCoords = null)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not draw a triangle with a disposed texture.");

            RectangleF texRect = GetTextureRect(textureRect);
            Vector2 inflationAmount = inflationPercentage.HasValue ? new Vector2(inflationPercentage.Value.X * texRect.Width, inflationPercentage.Value.Y * texRect.Height) : Vector2.Zero;

            // If clamp to edge is active, allow the texture coordinates to penetrate by half the repeated atlas margin width
            if (Vd.CurrentWrapModeS == WrapMode.ClampToEdge || Vd.CurrentWrapModeT == WrapMode.ClampToEdge)
            {
                Vector2 inflationVector = Vector2.Zero;

                const int mipmap_padding_requirement = (1 << MAX_MIPMAP_LEVELS) / 2;

                if (Vd.CurrentWrapModeS == WrapMode.ClampToEdge)
                    inflationVector.X = mipmap_padding_requirement / (float)width;
                if (Vd.CurrentWrapModeT == WrapMode.ClampToEdge)
                    inflationVector.Y = mipmap_padding_requirement / (float)height;
                texRect = texRect.Inflate(inflationVector);
            }

            RectangleF coordRect = GetTextureRect(textureCoords ?? textureRect);
            RectangleF inflatedCoordRect = coordRect.Inflate(inflationAmount);

            vertexAction ??= default_quad_action;

            // We split the triangle into two, such that we can obtain smooth edges with our
            // texture coordinate trick. We might want to revert this to drawing a single
            // triangle in case we ever need proper texturing, or if the additional vertices
            // end up becoming an overhead (unlikely).
            SRGBColour topColour = (drawColour.TopLeft + drawColour.TopRight) / 2;
            SRGBColour bottomColour = (drawColour.BottomLeft + drawColour.BottomRight) / 2;

            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P0,
                TexturePosition = new Vector2((inflatedCoordRect.Left + inflatedCoordRect.Right) / 2, inflatedCoordRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = topColour.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P1,
                TexturePosition = new Vector2(inflatedCoordRect.Left, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = drawColour.BottomLeft.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = (vertexTriangle.P1 + vertexTriangle.P2) / 2,
                TexturePosition = new Vector2((inflatedCoordRect.Left + inflatedCoordRect.Right) / 2, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = bottomColour.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P2,
                TexturePosition = new Vector2(inflatedCoordRect.Right, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = drawColour.BottomRight.Linear,
            });

            FrameStatistics.Add(StatisticsCounterType.Pixels, (long)vertexTriangle.Area);
        }

        public const int VERTICES_PER_QUAD = 4;

        internal override void DrawQuad(Quad vertexQuad, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null,
                                        Vector2? blendRangeOverride = null, RectangleF? textureCoords = null)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not draw a quad with a disposed texture.");

            RectangleF texRect = GetTextureRect(textureRect);
            Vector2 inflationAmount = inflationPercentage.HasValue ? new Vector2(inflationPercentage.Value.X * texRect.Width, inflationPercentage.Value.Y * texRect.Height) : Vector2.Zero;

            // If clamp to edge is active, allow the texture coordinates to penetrate by half the repeated atlas margin width
            if (Vd.CurrentWrapModeS == WrapMode.ClampToEdge || Vd.CurrentWrapModeT == WrapMode.ClampToEdge)
            {
                Vector2 inflationVector = Vector2.Zero;

                const int mipmap_padding_requirement = (1 << MAX_MIPMAP_LEVELS) / 2;

                if (Vd.CurrentWrapModeS == WrapMode.ClampToEdge)
                    inflationVector.X = mipmap_padding_requirement / (float)width;
                if (Vd.CurrentWrapModeT == WrapMode.ClampToEdge)
                    inflationVector.Y = mipmap_padding_requirement / (float)height;
                texRect = texRect.Inflate(inflationVector);
            }

            RectangleF coordRect = GetTextureRect(textureCoords ?? textureRect);
            RectangleF inflatedCoordRect = coordRect.Inflate(inflationAmount);
            Vector2 blendRange = blendRangeOverride ?? inflationAmount;

            vertexAction ??= default_quad_action;

            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.BottomLeft,
                TexturePosition = new Vector2(inflatedCoordRect.Left, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.BottomLeft.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.BottomRight,
                TexturePosition = new Vector2(inflatedCoordRect.Right, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.BottomRight.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.TopRight,
                TexturePosition = new Vector2(inflatedCoordRect.Right, inflatedCoordRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.TopRight.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.TopLeft,
                TexturePosition = new Vector2(inflatedCoordRect.Left, inflatedCoordRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.TopLeft.Linear,
            });

            FrameStatistics.Add(StatisticsCounterType.Pixels, (long)vertexQuad.Area);
        }

        internal override void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? uploadOpacity)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not set data of a disposed texture.");

            if (upload.Bounds.IsEmpty && upload.Data.Length > 0)
            {
                upload.Bounds = Bounds;
                if (width * height > upload.Data.Length)
                    throw new InvalidOperationException($"Size of texture upload ({width}x{height}) does not contain enough data ({upload.Data.Length} < {width * height})");
            }

            UpdateOpacity(upload, ref uploadOpacity);

            lock (uploadQueue)
            {
                bool requireUpload = uploadQueue.Count == 0;
                uploadQueue.Enqueue(upload);

                if (requireUpload && !BypassTextureUploadQueueing)
                    Vd.EnqueueTextureUpload(this);
            }
        }

        internal override bool Bind(WrapMode wrapModeS, WrapMode wrapModeT)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed texture.");

            Upload();

            if (texture == null)
                return false;

            if (Vd.BindTexture(this, wrapModeS, wrapModeT))
                BindCount++;

            return true;
        }

        /// <summary>
        /// Whether this <see cref="VeldridTextureSingle"/> generates mipmaps manually.
        /// </summary>
        private readonly bool manualMipmaps;

        /// <summary>
        /// Whether the current texture has mipmaps populated from a texture upload.
        /// </summary>
        private bool textureHasMipmaps;

        internal override bool Upload()
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

            if (didUpload && !manualMipmaps && !textureHasMipmaps)
                Vd.Commands.GenerateMipmaps(texture);

            return didUpload;
        }

        internal override void FlushUploads()
        {
            while (tryGetNextUpload(out var upload))
                upload.Dispose();
        }

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

        protected virtual unsafe void DoUpload(ITextureUpload upload)
        {
            // Do we need to generate a new sampler?
            if (sampler == null)
            {
                var samplerDescription = new SamplerDescription
                {
                    AddressModeU = SamplerAddressMode.Clamp,
                    AddressModeV = SamplerAddressMode.Clamp,
                    AddressModeW = SamplerAddressMode.Clamp,
                    Filter = filteringMode.ToSamplerFilter(manualMipmaps),
                    LodBias = 0,
                    MinimumLod = 0,
                    MaximumLod = MAX_MIPMAP_LEVELS,
                    MaximumAnisotropy = 0
                };

                sampler = Vd.Factory.CreateSampler(samplerDescription);
            }

            // Do we need to generate a new texture?
            if (texture == null || texture.Width != width || texture.Height != height)
            {
                textureResourceSet?.Dispose();
                texture?.Dispose();

                textureHasMipmaps = false;

                var usage = TextureUsage.Sampled;

                if (!manualMipmaps)
                    usage |= TextureUsage.GenerateMipmaps;

                var textureDescription = TextureDescription.Texture2D((uint)width, (uint)height, (uint)calculateMipmapLevels(width, height), 1, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, usage);

                texture = Vd.Factory.CreateTexture(textureDescription);
                textureResourceSet = new TextureResourceSet(texture, sampler);

                Vd.BindTexture(this);

                if (!upload.Data.IsEmpty)
                    Vd.UpdateTexture(texture, upload.Bounds.X, upload.Bounds.Y, upload.Bounds.Width, upload.Bounds.Height, upload.Level, upload.Data);
            }
            // Just update content of the current texture
            else if (!upload.Data.IsEmpty)
            {
                Vd.BindTexture(this);

                if (upload.Level > 0)
                    textureHasMipmaps = true;

                int div = (int)Math.Pow(2, upload.Level);

                Vd.UpdateTexture(texture, upload.Bounds.X / div, upload.Bounds.Y / div, upload.Bounds.Width / div, upload.Bounds.Height / div, upload.Level, upload.Data);
            }
        }

        // private unsafe void initializeLevel(int level, int width, int height)
        // {
        //     using (var image = createBackingImage(width, height))
        //     using (var pixels = image.CreateReadOnlyPixelSpan())
        //     {
        //         updateMemoryUsage(level, (long)width * height * sizeof(Rgba32));
        //         Vd.UpdateTexture(texture, 0, 0, width, height, level, pixels.Span);
        //     }
        // }
        //
        // private Image<Rgba32> createBackingImage(int width, int height)
        // {
        //     // it is faster to initialise without a background specification if transparent black is all that's required.
        //     return initialisationColour == default
        //         ? new Image<Rgba32>(width, height)
        //         : new Image<Rgba32>(width, height, initialisationColour);
        // }

        private static int calculateMipmapLevels(int width, int height) => Math.Min(1 + (int)Math.Floor(Math.Log(Math.Max(width, height), 2)), MAX_MIPMAP_LEVELS);
    }
}
