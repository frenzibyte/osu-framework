// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;
using osu.Framework.Graphics.Renderer.Textures;
using osu.Framework.Graphics.Textures;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;
using Vector2 = osuTK.Vector2;
using Texture = Veldrid.Texture;

namespace osu.Framework.Graphics.Renderer.Buffers
{
    public class FrameBuffer : IDisposable
    {
        private Framebuffer frameBuffer;

        public RendererTexture Texture { get; private set; }

        private bool isInitialised;

        private readonly PixelFormat[] colorFormats;
        private readonly PixelFormat? depthFormat;
        private readonly FilteringMode filteringMode;

        private Texture[] colorTargets;
        private Texture depthTarget;

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
                {
                    Texture.Width = (int)Math.Ceiling(size.X);
                    Texture.Height = (int)Math.Ceiling(size.Y);

                    Texture.SetData(new TextureUpload());
                    Texture.Upload();
                }
            }
        }

        private void initialise()
        {
            setupRenderTargets();

            frameBuffer = Vd.Factory.CreateFramebuffer(new FramebufferDescription(depthTarget, colorTargets));

            Vd.BindFrameBuffer(frameBuffer);
            Vd.BindDefaultTexture();
        }

        private void setupRenderTargets()
        {
            Texture = new FrameBufferTexture(Size, filteringMode);

            colorTargets = new Texture[1 + (colorFormats?.Length ?? 0)];
            colorTargets[0] = Texture.TextureResourceSet.Texture;

            if (colorFormats != null)
            {
                for (int i = 0; i < colorFormats.Length; i++)
                {
                    var description = TextureDescription.Texture2D((uint)Size.X, (uint)Size.Y, 1, 1, colorFormats[i], TextureUsage.RenderTarget);
                    colorTargets[1 + i] = Vd.Factory.CreateTexture(description);
                }
            }

            if (depthFormat != null)
            {
                var description = TextureDescription.Texture2D((uint)Size.X, (uint)Size.Y, 1, 1, depthFormat.Value, TextureUsage.DepthStencil);
                depthTarget = Vd.Factory.CreateTexture(description);
            }
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
            else
            {
                // Buffer is bound during initialisation
                Vd.BindFrameBuffer(frameBuffer);
            }
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
                Texture?.Dispose();
                Texture = null;

                Vd.DeleteFrameBuffer(frameBuffer);
            }

            isDisposed = true;
        }

        #endregion

        private class FrameBufferTexture : RendererTextureSingle
        {
            public FrameBufferTexture(Vector2 size, FilteringMode filteringMode = FilteringMode.Linear)
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
