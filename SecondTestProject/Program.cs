// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework;
using osu.Framework.Configuration;
using osu.Framework.Development;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osuTK.Graphics;
using Texture = osu.Framework.Graphics.Textures.Texture;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;

namespace FirstTestProject
{
    public static class Program
    {
        private static SDL2DesktopWindow window;

        [STAThread]
        public static void Main()
        {
            window = new SDL2DesktopWindow();
            window.SetupWindow(new FrameworkConfigManager(new NativeStorage("~/.local/share/osu-framework-veldrid")));
            window.Create();

            window.Visible = true;
            window.Title = "osu!framework (running under Veldrid)";

            var shaderManager = new ShaderManager(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Game).Assembly), "Resources/Shaders"));

            var texture = Texture.FromStream(File.OpenRead("/Users/salman/Desktop/osu-framework-veldrid/osu.Framework.Tests/Resources/Textures/sample-texture.png"));
            var shader = shaderManager.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);

            window.Update += () =>
            {
                ThreadSafety.IsDrawThread = true;

                Vd.Commands.Begin();

                Vd.Reset(new System.Numerics.Vector2(window.Size.Width, window.Size.Height));

                shader.Bind();

                texture.DrawQuad(new Quad(-0.75f, -0.75f, 1.5f, 1.5f), Color4.White);

                Vd.FlushCurrentBatch();

                shader.Unbind();

                Vd.Commands.End();
                Vd.Device.SubmitCommands(Vd.Commands);
                Vd.Device.SwapBuffers();

                ThreadSafety.IsDrawThread = false;
            };

            window.Run();
        }
    }
}
