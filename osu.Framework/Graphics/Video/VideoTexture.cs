// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Renderer.Textures;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using Veldrid;
using Texture = Veldrid.Texture;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;

namespace osu.Framework.Graphics.Video
{
    internal unsafe class VideoTexture : RendererTextureSingle
    {
        private TextureResourceSet textureResourceSet;

        /// <summary>
        /// Whether the latest frame data has been uploaded.
        /// </summary>
        public bool UploadComplete { get; private set; }

        public VideoTexture(int width, int height, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
            : base(width, height, true, FilteringMode.Linear, wrapModeS, wrapModeT)
        {
        }

        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        internal override void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? uploadOpacity)
        {
            if (uploadOpacity != null && uploadOpacity != Opacity.Opaque)
                throw new InvalidOperationException("Video texture uploads must always be opaque");

            UploadComplete = false;

            // We do not support videos with transparency at this point,
            // so the upload's opacity as well as the texture's opacity
            // is always opaque.
            base.SetData(upload, wrapModeS, wrapModeT, Opacity = Opacity.Opaque);
        }

        public override TextureResourceSet TextureResourceSet => textureResourceSet;

        private int textureSize;

        public override int GetByteSize() => textureSize;

        internal override bool Bind(WrapMode wrapModeS, WrapMode wrapModeT)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed texture.");

            Upload();

            if (textureResourceSet == null)
                return false;

            if (Vd.BindTexture(textureResourceSet, wrapModeS, wrapModeT))
                BindCount++;

            return true;
        }

        protected override void DoUpload(ITextureUpload upload)
        {
            if (!(upload is VideoTextureUpload videoUpload))
                return;

            // Do we need to generate a new texture?
            if (textureResourceSet == null)
            {
                Debug.Assert(memoryLease == null);
                memoryLease = NativeMemoryTracker.AddMemory(this, Width * Height * 3 / 2);

                var textures = new Texture[3];

                for (int i = 0; i < textures.Length; i++)
                {
                    int width, height;

                    if (i == 0)
                    {
                        width = videoUpload.Frame->width;
                        height = videoUpload.Frame->height;

                        textureSize += width * height;
                    }
                    else
                    {
                        width = (videoUpload.Frame->width + 1) / 2;
                        height = (videoUpload.Frame->height + 1) / 2;

                        textureSize += width * height;
                    }

                    textures[i] = Vd.Factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8_UNorm, TextureUsage.Sampled));
                }

                textureResourceSet = new TextureResourceSet(textures, Vd.Device.LinearSampler);
            }

            Vd.BindTexture(TextureResourceSet);

            for (int i = 0; i < TextureResourceSet.Textures.Count; i++)
            {
                int width = videoUpload.Frame->width / (i > 0 ? 2 : 1);
                int height = videoUpload.Frame->height / (i > 0 ? 2 : 1);
                var data = new ReadOnlySpan<byte>(videoUpload.Frame->data[(uint)i], width * height);
                Vd.UpdateTexture(TextureResourceSet.Textures[i], 0, 0, width, height, 0, data);

                // GL.PixelStore(PixelStoreParameter.UnpackRowLength, videoUpload.Frame->linesize[(uint)i]);
                // GL.TexSubImage2D(TextureTarget2d.Texture2D, 0, 0, 0, videoUpload.Frame->width / (i > 0 ? 2 : 1), videoUpload.Frame->height / (i > 0 ? 2 : 1),
                //     PixelFormat.Red, PixelType.UnsignedByte, (IntPtr)videoUpload.Frame->data[(uint)i]);
            }

            UploadComplete = true;
        }

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            memoryLease?.Dispose();

            Vd.ScheduleDisposal(v =>
            {
                if (v.textureResourceSet == null)
                    return;

                foreach (var texture in v.textureResourceSet.Textures)
                    texture.Dispose();

                v.textureResourceSet.Dispose();
                v.textureResourceSet = null;
            }, this);
        }

        #endregion
    }
}
