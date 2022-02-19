// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Textures;
using osuTK;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    public class FrameBuffer : IDisposable
    {
        private Framebuffer frameBuffer;

        public VeldridTexture VeldridTexture { get; private set; }

        private bool isInitialised;

        private readonly PixelFormat[] colorFormats;
        private readonly PixelFormat? depthFormat;
        private readonly FilteringMode filteringMode;

        public FrameBuffer(PixelFormat[] colorFormats = null, PixelFormat? depthFormat = null, FilteringMode filteringMode = FilteringMode.Linear)
        {
            this.colorFormats = colorFormats;
            this.depthFormat = depthFormat;
            this.filteringMode = filteringMode;
        }

        private Vector2 size = Vector2.One;

        /// <summary>
        /// Sets the size of the texture of this frame buffer.
        /// </summary>
        public Vector2 Size
        {
            get => size;
            set
            {
                if (value == size)
                    return;

                size = value;

                if (isInitialised)
                    // reinitialise to update framebuffer size.
                    initialise();
            }
        }

        private void initialise()
        {
            VeldridTexture?.Dispose();
            frameBuffer?.Dispose();

            var description = new FramebufferDescription();

            VeldridTexture = new FrameBufferVeldridTexture(Size, filteringMode);

            description.ColorTargets = new FramebufferAttachmentDescription[1 + (colorFormats?.Length ?? 0)];
            description.ColorTargets[0] = new FramebufferAttachmentDescription(VeldridTexture.TextureResourceSet.Texture, 0);

            if (colorFormats != null)
            {
                for (int i = 0; i < colorFormats.Length; i++)
                {
                    var targetDescription = TextureDescription.Texture2D((uint)Size.X, (uint)Size.Y, 1, 1, colorFormats[i], TextureUsage.RenderTarget);
                    description.ColorTargets[1 + i] = new FramebufferAttachmentDescription(Vd.Factory.CreateTexture(targetDescription), 0);
                }
            }

            if (depthFormat != null)
            {
                var targetDescription = TextureDescription.Texture2D((uint)Size.X, (uint)Size.Y, 1, 1, depthFormat.Value, TextureUsage.DepthStencil);
                description.DepthTarget = new FramebufferAttachmentDescription(Vd.Factory.CreateTexture(targetDescription), 0);
            }

            frameBuffer = Vd.Factory.CreateFramebuffer(description);
        }

        /// <summary>
        /// Binds the framebuffer.
        /// <para>Does not clear the buffer or reset the viewport/ortho.</para>
        /// </summary>
        public void Bind()
        {
            if (!isInitialised)
            {
                initialise();
                isInitialised = true;
            }

            Vd.BindFrameBuffer(frameBuffer);
        }

        /// <summary>
        /// Unbinds the framebuffer.
        /// </summary>
        public void Unbind() => Vd.UnbindFrameBuffer(frameBuffer);

        #region Disposal

        ~FrameBuffer()
        {
            Vd.ScheduleDisposal(b => b.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            if (isInitialised)
            {
                VeldridTexture?.Dispose();
                VeldridTexture = null;

                Vd.UnbindFrameBuffer(frameBuffer);

                Vd.ScheduleDisposal(f =>
                {
                    for (int i = 0; i < f.frameBuffer.ColorTargets.Count; i++)
                        f.frameBuffer.ColorTargets[i].Target.Dispose();

                    f.frameBuffer.DepthTarget?.Target.Dispose();

                    f.frameBuffer.Dispose();
                    f.frameBuffer = null;
                }, this);
            }

            isDisposed = true;
        }

        #endregion

        private class FrameBufferVeldridTexture : VeldridTextureSingle
        {
            public FrameBufferVeldridTexture(Vector2 size, FilteringMode filteringMode = FilteringMode.Linear)
                : base((int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y), true, filteringMode)
            {
                BypassTextureUploadQueueing = true;

                SetData(new TextureUpload());
                Upload();
            }

            public override int Width
            {
                get => base.Width;
                set => base.Width = Math.Clamp(value, 1, Vd.MaxTextureSize);
            }

            public override int Height
            {
                get => base.Height;
                set => base.Height = Math.Clamp(value, 1, Vd.MaxTextureSize);
            }
        }
    }
}
