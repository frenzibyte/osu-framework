// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Utils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneCursorContainer : ManualInputManagerTestScene
    {
        private Container container;
        private TestCursorContainer cursorContainer;

        [SetUp]
        public new void SetUp() => Schedule(createContent);

        [Test]
        public void TestPositionalUpdate()
        {
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(container.ScreenSpaceDrawQuad.Centre));
            AddAssert("cursor is centered", () => cursorCenteredInContainer());
            AddAssert("cursor at mouse position", () => cursorAtMouseScreenSpace());
        }

        [Test]
        public void TestContainerMovement()
        {
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(container.ScreenSpaceDrawQuad.Centre));
            AddAssert("cursor is centered", () => cursorCenteredInContainer());
            AddStep("Move container", () => container.Y += 50);
            AddAssert("cursor no longer centered", () => !cursorCenteredInContainer());
            AddAssert("cursor at mouse position", () => cursorAtMouseScreenSpace());
            AddStep("Resize container", () => container.Size *= new Vector2(1.4f, 1));
            AddAssert("cursor at mouse position", () => cursorAtMouseScreenSpace());
        }

        /// <summary>
        /// Ensures we receive position updates from <see cref="IRequireHighFrequencyMousePosition"/> while mouse is staying still.
        /// </summary>
        [Test]
        public void TestContainerRecreation()
        {
            AddStep("Move cursor to test centre", () => InputManager.MoveMouseTo(Content.ScreenSpaceDrawQuad.Centre));
            AddStep("Recreate container with mouse already in place", createContent);
            AddAssert("cursor is centered", () => cursorCenteredInContainer());
            AddAssert("cursor at mouse position", () => cursorAtMouseScreenSpace());
        }

        /// <summary>
        /// Ensures the cursor hides when mouse input from touch source is applied.
        /// </summary>
        [Test]
        public void TestTouchInputSource()
        {
            AddStep("Begin touch at test centre", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, Content.ScreenSpaceDrawQuad.Centre)));
            AddAssert("cursor is hidden", () => cursorContainer.ActiveCursor.Alpha == 0);
            AddStep("Move with mouse", () => InputManager.MoveMouseTo(Content, new Vector2(10)));
            AddAssert("cursor is visible", () => cursorContainer.ActiveCursor.Alpha == 1);
            AddAssert("cursor at mouse position", () => cursorAtMouseScreenSpace());
            AddStep("Move with touch", () => InputManager.MoveTouchTo(new Touch(TouchSource.Touch1, Content.ScreenSpaceDrawQuad.Centre + new Vector2(20))));
            AddAssert("cursor is hidden back", () => cursorContainer.ActiveCursor.Alpha == 0);
        }

        private bool cursorCenteredInContainer() =>
            Precision.AlmostEquals(
                cursorContainer.ActiveCursor.ScreenSpaceDrawQuad.Centre,
                container.ScreenSpaceDrawQuad.Centre);

        private bool cursorAtMouseScreenSpace() =>
            Precision.AlmostEquals(
                cursorContainer.ActiveCursor.ScreenSpaceDrawQuad.Centre,
                InputManager.CurrentState.Mouse.Position);

        private void createContent()
        {
            Child = container = new Container
            {
                Masking = true,
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(0.5f),
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Yellow,
                        RelativeSizeAxes = Axes.Both,
                    },
                    cursorContainer = new TestCursorContainer
                    {
                        Name = "test",
                        RelativeSizeAxes = Axes.Both
                    }
                }
            };
        }

        private class TestCursorContainer : CursorContainer
        {
            protected override Drawable CreateCursor() => new Circle
            {
                Size = new Vector2(50),
                Colour = Color4.Red,
                Origin = Anchor.Centre,
            };
        }
    }
}
