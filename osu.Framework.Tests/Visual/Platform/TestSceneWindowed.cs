// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("This test cannot be run in headless mode (a window instance is required).")]
    public class TestSceneWindowed : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        private SDL2DesktopWindow sdlWindow;

        [BackgroundDependencyLoader]
        private void load()
        {
            if (!(host.Window is SDL2DesktopWindow window))
                return;

            sdlWindow = window;
        }

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            setWindowMinSize(new Size(640, 480));
            setWindowMaxSize(new Size(int.MaxValue, int.MaxValue));
            sdlWindow.Resizable = true;
        });

        [Test]
        public void TestToggleResizable()
        {
            AddToggleStep("toggle resizable", state => sdlWindow.Resizable = state);
        }

        [Test]
        public void TestMinimumSize()
        {
            const int min_width = 1024;
            const int min_height = 768;

            AddStep("set client size 640x480", () => setWindowSize(new Size(640, 480)));
            AddStep($"set minimum size to {min_width}x{min_height}", () => setWindowMinSize(new Size(min_width, min_height)));
            assertWindowSize(new Size(min_width, min_height));

            AddStep("set client size 1280x720", () => setWindowSize(new Size(1280, 720)));
            AddStep("set client size 640x480", () => setWindowSize(new Size(640, 480)));
            assertWindowSize(new Size(min_width, min_height));
        }

        [Test]
        public void TestMaximumSize()
        {
            const int max_width = 1024;
            const int max_height = 768;

            AddStep("set client size to 1280x720", () => setWindowSize(new Size(1280, 720)));
            AddStep($"set maximum size to {max_width}x{max_height}", () => setWindowMaxSize(new Size(max_width, max_height)));
            assertWindowSize(new Size(max_width, max_height));

            AddStep("set client size 640x480", () => setWindowSize(new Size(640, 480)));
            AddStep("set client size 1280x720", () => setWindowSize(new Size(1280, 720)));
            assertWindowSize(new Size(max_width, max_height));
        }

        private void setWindowSize(Size size) => config.SetValue(FrameworkSetting.WindowedSize, (size / sdlWindow.Scale).ToSize());
        private void setWindowMinSize(Size minSize) => ((BindableSize)config.GetBindable<Size>(FrameworkSetting.WindowedSize)).MinValue = minSize;
        private void setWindowMaxSize(Size maxSize) => ((BindableSize)config.GetBindable<Size>(FrameworkSetting.WindowedSize)).MaxValue = maxSize;

        private void assertWindowSize(Size size)
        {
            AddAssert($"client size = {size.Width}x{size.Height}", () => sdlWindow.ClientSize == (size * sdlWindow.Scale).ToSize());
            AddAssert($"size in config = {size.Width}x{size.Height}", () => config.Get<Size>(FrameworkSetting.WindowedSize) == size);
        }
    }
}
