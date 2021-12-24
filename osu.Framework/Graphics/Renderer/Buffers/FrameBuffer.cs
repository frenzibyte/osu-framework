// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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

        private readonly FilteringMode filteringMode;
        private readonly PixelFormat[] renderBufferFormats;

        private readonly List<Texture> colorTargets = new List<Texture>();

        private Texture depthTarget;

        public FrameBuffer(PixelFormat[] renderBufferFormats = null, FilteringMode filteringMode = FilteringMode.Linear)
        {
            this.renderBufferFormats = renderBufferFormats;
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
            if (renderBufferFormats != null)
            {
                foreach (var format in renderBufferFormats)
                {
                    // todo: this should just be separated, rather than bullshit.
                    bool isDepthStencil = format == PixelFormat.R16_UNorm || format == PixelFormat.D24_UNorm_S8_UInt || format == PixelFormat.D32_Float_S8_UInt;
                    var usage = isDepthStencil ? TextureUsage.DepthStencil : TextureUsage.RenderTarget;

                    var description = TextureDescription.Texture2D((uint)Size.X, (uint)Size.Y, 1, 1, format, usage);
                    var texture = Vd.Factory.CreateTexture(description);

                    if (isDepthStencil)
                        depthTarget = texture;
                    else
                        colorTargets.Add(texture);
                }
            }

            frameBuffer = Vd.Factory.CreateFramebuffer(new FramebufferDescription(depthTarget, colorTargets.ToArray()));
            Texture = new FrameBufferTexture(Size, filteringMode);

            Vd.BindFrameBuffer(frameBuffer);
            Vd.BindTexture((RendererTexture)null);
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
        public void Unbind()
        {
            // // See: https://community.arm.com/developer/tools-software/graphics/b/blog/posts/mali-performance-2-how-to-correctly-handle-framebuffers
            // // Unbinding renderbuffers causes an invalidation of the relevant attachment of this framebuffer on embedded devices, causing the renderbuffers to remain transient.
            // // This must be done _before_ the framebuffer is flushed via the framebuffer unbind process, otherwise the renderbuffer may be copied to system memory.
            // foreach (var buffer in attachedRenderBuffers)
            //     buffer.Unbind();

            Vd.UnbindFrameBuffer(frameBuffer);
        }

        #region Disposal

        ~FrameBuffer()
        {
            Vd.ScheduleDisposal(() => Dispose(false));
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
