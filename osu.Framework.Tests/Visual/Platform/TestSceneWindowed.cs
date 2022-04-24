﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using NUnit.Framework;
using osu.Framework.Allocation;
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
            sdlWindow.MinSize = new Size(640, 480);
            sdlWindow.MaxSize = new Size(9999, 9999);
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
            AddStep("set client size 640x480", () => setWindowSize(new Size(640, 480)));
            AddStep("set minimum size to 1024x768", () => sdlWindow.MinSize = new Size(1024, 768));
            assertWindowSize(new Size(1024, 768));

            AddStep("set client size 1440x900", () => setWindowSize(new Size(1440, 900)));
            AddStep("set client size 640x480", () => setWindowSize(new Size(640, 480)));
            assertWindowSize(new Size(1024, 768));

            AddStep("overlapping size throws", () => Assert.Throws<InvalidOperationException>(() => sdlWindow.MinSize = sdlWindow.MaxSize + new Size(1, 1)));
            AddStep("negative size throws", () => Assert.Throws<InvalidOperationException>(() => sdlWindow.MinSize = new Size(-500, -500)));
        }

        [Test]
        public void TestMaximumSize()
        {
            AddStep("set client size to 1024x768", () => setWindowSize(new Size(1024, 768)));
            AddStep("set maximum size to 720x720", () => sdlWindow.MaxSize = new Size(720, 720));
            assertWindowSize(new Size(720, 720));

            AddStep("set client size 640x480", () => setWindowSize(new Size(640, 480)));
            AddStep("set client size 1024x768", () => setWindowSize(new Size(1024, 768)));
            assertWindowSize(new Size(720, 720));

            AddStep("overlapping size throws", () => Assert.Throws<InvalidOperationException>(() => sdlWindow.MaxSize = sdlWindow.MinSize - new Size(1, 1)));
            AddStep("negative size throws", () => Assert.Throws<InvalidOperationException>(() => sdlWindow.MaxSize = new Size(-1, -1)));
            AddStep("zero size throws", () => Assert.Throws<InvalidOperationException>(() => sdlWindow.MaxSize = new Size(0, 0)));
        }

        private void setWindowSize(Size clientSize) => config.SetValue(FrameworkSetting.WindowedSize, (clientSize / sdlWindow.Scale).ToSize());

        private void assertWindowSize(Size clientSize)
        {
            AddAssert($"client size = {clientSize.Width}x{clientSize.Height}", () => sdlWindow.ClientSize == clientSize);
            AddAssert($"size in config = {clientSize.Width}x{clientSize.Height}", () => config.Get<Size>(FrameworkSetting.WindowedSize) == (clientSize / sdlWindow.Scale).ToSize());
        }
    }
}
