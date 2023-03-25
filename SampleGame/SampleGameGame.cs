// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Buffers;
using Veldrid;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        private Box box = null!;
        private IShader shader = null!;
        private VeldridFrameBuffer? frameBuffer;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(300, 300),
                Colour = Color4.Green
            });
            // Add(new BufferedContainer(cachedFrameBuffer: false)
            // {
            //     AutoSizeAxes = Axes.Both,
            // });
            // Add(box = new Box
            // {
            //     Anchor = Anchor.Centre,
            //     Origin = Anchor.Centre,
            //     Size = new Vector2(150, 150),
            //     Colour = Color4.Tomato
            // });

            shader = Shaders.Load("Texture2D", "Texture");
        }

        protected override void Update()
        {
            base.Update();
            box.Rotation += (float)Time.Elapsed / 10;
        }

        public override void Draw(IRenderer renderer)
        {
            frameBuffer ??= (VeldridFrameBuffer)renderer.CreateFrameBuffer();

            var veldrid = (VeldridRenderer)renderer;
            veldrid.Commands.Begin();

            veldrid.BindShader(shader);
            veldrid.DrawQuad(veldrid.WhitePixel, new Quad(-0.75f, -0.75f, 0.75f * 2, 0.75f * 2), Color4.Red);
            veldrid.UnbindShader(shader);

            veldrid.FlushCurrentBatch(FlushBatchSource.SetShader);

            veldrid.Commands.SetFramebuffer(frameBuffer.Framebuffer);
            veldrid.Commands.SetFramebuffer(veldrid.Device.SwapchainFramebuffer);

            veldrid.BindShader(shader);
            veldrid.DrawQuad(veldrid.WhitePixel, new Quad(-0.25f, -0.25f, 0.25f * 2, 0.25f * 2), Color4.Blue);
            veldrid.UnbindShader(shader);

            veldrid.FlushCurrentBatch(FlushBatchSource.SetShader);

            veldrid.Commands.End();
            veldrid.Device.SubmitCommands(veldrid.Commands);
            veldrid.Device.SwapBuffers();
        }
    }
}
