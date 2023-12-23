// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestSceneFocus : ManualInputManagerTestScene
    {
        private FocusOverlay overlay;
        private RequestingFocusBox requestingFocus;

        private FocusBox focusTopLeft;
        private FocusBox focusBottomLeft;
        private FocusBox focusBottomRight;

        public TestSceneFocus()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
            Children = new Drawable[]
            {
                focusTopLeft = new FocusBox
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                },
                requestingFocus = new RequestingFocusBox
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                },
                focusBottomLeft = new FocusBox
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                },
                focusBottomRight = new FocusBox
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                },
                overlay = new FocusOverlay
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            };
        });

        [Test]
        public void TestFocusedOverlayTakesFocusOnShow()
        {
            AddAssert("overlay not visible", () => overlay.State.Value == Visibility.Hidden);
            checkNotFocused(() => overlay);

            AddStep("show overlay", () => overlay.Show());
            checkFocused(() => overlay);

            AddStep("hide overlay", () => overlay.Hide());
            checkNotFocused(() => overlay);
        }

        [Test]
        public void TestFocusedOverlayLosesFocusOnClickAway()
        {
            AddAssert("overlay not visible", () => overlay.State.Value == Visibility.Hidden);
            checkNotFocused(() => overlay);

            AddStep("show overlay", () => overlay.Show());
            checkFocused(() => overlay);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.One);
                InputManager.Click(MouseButton.Left);
            });

            checkNotFocused(() => overlay);
            checkFocused(() => requestingFocus);
        }

        [Test]
        public void TestRequestsFocusKeepsFocusOnClickAway()
        {
            checkFocused(() => requestingFocus);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.One);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => requestingFocus);
        }

        [Test]
        public void TestRequestsFocusLosesFocusOnClickingFocused()
        {
            checkFocused(() => requestingFocus);

            AddStep("click top left", () =>
            {
                InputManager.MoveMouseTo(focusTopLeft);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => focusTopLeft);

            AddStep("click bottom right", () =>
            {
                InputManager.MoveMouseTo(focusBottomRight);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => focusBottomRight);
        }

        /// <summary>
        /// Ensures that performing <see cref="InputManager.ChangeFocus(Drawable)"/> to a drawable with disabled <see cref="Drawable.AcceptsSubtreeFocus"/> returns <see langword="false"/>.
        /// </summary>
        [Test]
        public void TestDisabledFocusDrawableCannotReceiveFocusViaChangeFocus()
        {
            checkFocused(() => requestingFocus);

            AddStep("disable focus from top left", () => focusTopLeft.AllowAcceptingFocus = false);
            AddAssert("cannot switch focus to top left", () => !InputManager.ChangeFocus(focusTopLeft));

            checkFocused(() => requestingFocus);
        }

        /// <summary>
        /// Ensures that performing <see cref="InputManager.ChangeFocus(Drawable)"/> to a non-present drawable returns <see langword="false"/>.
        /// </summary>
        [Test]
        public void TestNotPresentDrawableCannotReceiveFocusViaChangeFocus()
        {
            checkFocused(() => requestingFocus);

            AddStep("hide top left", () => focusTopLeft.Alpha = 0);
            AddAssert("cannot switch focus to top left", () => !InputManager.ChangeFocus(focusTopLeft));

            checkFocused(() => requestingFocus);
        }

        /// <summary>
        /// Ensures that performing <see cref="InputManager.ChangeFocus(Drawable)"/> to a drawable of a non-present parent returns <see langword="false"/>.
        /// </summary>
        [Test]
        public void TestDrawableOfNotPresentParentCannotReceiveFocusViaChangeFocus()
        {
            checkFocused(() => requestingFocus);

            AddStep("wrap top left in hidden container", () =>
            {
                Container container;

                Add(container = new Container
                {
                    Alpha = 0,
                    RelativeSizeAxes = Axes.Both,
                });

                Remove(focusTopLeft, false);
                container.Add(focusTopLeft);
            });
            AddAssert("cannot switch focus to top left", () => !InputManager.ChangeFocus(focusTopLeft));

            checkFocused(() => requestingFocus);
        }

        [Test]
        public void TestShowOverlayInteractions()
        {
            AddStep("click bottom left", () =>
            {
                InputManager.MoveMouseTo(focusBottomLeft);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => focusBottomLeft);

            AddStep("show overlay", () => overlay.Show());

            checkFocused(() => overlay);
            checkNotFocused(() => focusBottomLeft);

            // click is blocked by overlay so doesn't select bottom left first click
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => requestingFocus);

            // second click selects bottom left
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => focusBottomLeft);

            // further click has no effect
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => focusBottomLeft);
        }

        [Test]
        public void TestFocusPropagationViaRequest()
        {
            FocusBox parent = null;
            FocusBox child = null;

            AddStep("setup", () =>
            {
                Children = new[]
                {
                    parent = new RequestingFocusBox()
                        .With(f => f.Add(child = new FocusBox
                        {
                            AllowAcceptingFocus = false, // child does not need to accept focus
                            Size = new Vector2(0.5f),
                            Colour = Color4.Yellow,
                        })),
                };
            });

            checkFocused(() => parent);
            checkFocused(() => child);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.One);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => parent);
            checkFocused(() => child);
        }

        [Test]
        public void TestFocusPropagationViaChangeFocus()
        {
            FocusBox parent = null;
            FocusBox child = null;

            AddStep("setup", () =>
            {
                Children = new[]
                {
                    parent = new FocusBox()
                        .With(f => f.Add(child = new FocusBox
                        {
                            AllowAcceptingFocus = false, // child does not need to accept focus
                            Size = new Vector2(0.5f),
                            Colour = Color4.Yellow,
                        })),
                };
            });

            AddStep("focus parent", () => InputManager.ChangeFocus(parent));

            checkFocused(() => parent);
            checkFocused(() => child);
        }

        [Test]
        public void TestInputPropagation()
        {
            AddStep("Focus bottom left", () =>
            {
                InputManager.MoveMouseTo(focusBottomLeft);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("Press a key (blocking)", () =>
            {
                InputManager.PressKey(Key.A);
                InputManager.ReleaseKey(Key.A);
            });
            AddAssert("Received the key", () =>
                focusBottomLeft.KeyDownCount == 1 && focusBottomLeft.KeyUpCount == 1 &&
                focusBottomRight.KeyDownCount == 0 && focusBottomRight.KeyUpCount == 0);
            AddStep("Press a joystick (non blocking)", () =>
            {
                InputManager.PressJoystickButton(JoystickButton.Button1);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("Received the joystick button", () =>
                focusBottomLeft.JoystickPressCount == 1 && focusBottomLeft.JoystickReleaseCount == 1 &&
                focusBottomRight.JoystickPressCount == 1 && focusBottomRight.JoystickReleaseCount == 1);
        }

        [Test]
        public void TestSubtreeInputPropagation()
        {
            FocusBox parent = null;
            FocusBox child = null;
            FocusBox other = null;

            AddStep("setup", () =>
            {
                Children = new[]
                {
                    parent = new RequestingFocusBox()
                        .With(f => f.Add(child = new FocusBox
                        {
                            AllowAcceptingFocus = false, // child does not need to accept focus
                            Size = new Vector2(0.5f),
                            Colour = Color4.Yellow,
                        })),
                    other = new FocusBox
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                    },
                };
            });

            AddStep("press a key (blocking)", () => InputManager.Key(Key.A));

            AddAssert("only child received the key", () =>
                child.KeyDownCount == 1 && child.KeyUpCount == 1 &&
                parent.KeyDownCount == 0 && parent.KeyUpCount == 0 &&
                other.KeyDownCount == 0 && other.KeyUpCount == 0);

            AddStep("make child not block", () => child.AllowBlockingKeyDown = false);

            AddStep("press a key", () => InputManager.Key(Key.A));

            AddAssert("child and parent received the key", () =>
                child.KeyDownCount == 2 && child.KeyUpCount == 2 &&
                parent.KeyDownCount == 1 && parent.KeyUpCount == 1 &&
                other.KeyDownCount == 0 && other.KeyUpCount == 0);

            AddStep("press a joystick (non blocking)", () =>
            {
                InputManager.PressJoystickButton(JoystickButton.Button1);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });

            AddAssert("all received joystick button", () =>
                child.JoystickPressCount == 1 && child.JoystickReleaseCount == 1 &&
                parent.JoystickPressCount == 1 && parent.JoystickReleaseCount == 1 &&
                other.JoystickPressCount == 1 && other.JoystickReleaseCount == 1);
        }

        [Test]
        public void TestMoveFocusToChild()
        {
            FocusBox parent = null;
            FocusBox child = null;

            AddStep("setup", () =>
            {
                Children = new[]
                {
                    parent = new RequestingFocusBox()
                        .With(f => f.Add(child = new FocusBox
                        {
                            AllowAcceptingFocus = false, // child does not need to accept focus
                            Size = new Vector2(0.5f),
                            Colour = Color4.Yellow,
                        })),
                };
            });

            reset();

            AddStep("change focus to child", () =>
            {
                child.AllowAcceptingFocus = true;
                InputManager.ChangeFocus(child);
            });

            AddAssert("only parent received lost event", () =>
                parent.FocusCount == 0 && parent.FocusLostCount == 1 &&
                child.FocusCount == 0 && child.FocusLostCount == 0);

            reset();

            AddStep("change focus to parent", () => InputManager.ChangeFocus(parent));

            AddAssert("only parent received focus event", () =>
                parent.FocusCount == 1 && parent.FocusLostCount == 0 &&
                child.FocusCount == 0 && child.FocusLostCount == 0);

            void reset()
            {
                AddStep("reset count", () =>
                {
                    parent.FocusCount = parent.FocusLostCount = 0;
                    child.FocusCount = child.FocusLostCount = 0;
                });
            }
        }

        private void checkFocused(Func<Drawable> d) => AddAssert("check focus", () => d().HasFocus);
        private void checkNotFocused(Func<Drawable> d) => AddAssert("check not focus", () => !d().HasFocus);

        private partial class FocusOverlay : FocusedOverlayContainer
        {
            private readonly Box box;
            private readonly SpriteText stateText;

            public FocusOverlay()
            {
                RelativeSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Gray.Opacity(0.5f),
                    },
                    box = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.4f),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Blue,
                    },
                    new SpriteText
                    {
                        Text = "FocusedOverlay",
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                    stateText = new SpriteText
                    {
                        Text = "FocusedOverlay",
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                    }
                };

                this.FadeTo(0.2f);
            }

            protected override void PopIn()
            {
                stateText.Text = State.ToString();
            }

            protected override void PopOut()
            {
                stateText.Text = State.ToString();
            }

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

            protected override bool OnClick(ClickEvent e)
            {
                if (!box.ReceivePositionalInputAt(e.ScreenSpaceMousePosition))
                {
                    Hide();
                    return true;
                }

                return base.OnClick(e);
            }

            protected override void OnFocus(FocusEvent e)
            {
                base.OnFocus(e);
                this.FadeTo(1);
            }

            protected override void OnFocusLost(FocusLostEvent e)
            {
                base.OnFocusLost(e);
                this.FadeTo(0.2f);
            }
        }

        public partial class RequestingFocusBox : FocusBox
        {
            public override bool RequestsSubtreeFocus => true;

            public RequestingFocusBox()
            {
                Box.Colour = Color4.Green;

                AddInternal(new SpriteText
                {
                    Text = "RequestsFocus",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                });
            }
        }

        public partial class FocusBox : Container
        {
            protected Box Box;
            public int KeyDownCount, KeyUpCount, JoystickPressCount, JoystickReleaseCount, FocusCount, FocusLostCount;

            public FocusBox()
            {
                AddInternal(Box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.5f,
                    Colour = Color4.Red
                });

                RelativeSizeAxes = Axes.Both;
                Size = new Vector2(0.4f);
            }

            protected override bool OnClick(ClickEvent e) => true;

            public bool AllowAcceptingFocus = true;

            public override bool AcceptsSubtreeFocus => AllowAcceptingFocus;

            protected override void OnFocus(FocusEvent e)
            {
                base.OnFocus(e);
                Box.FadeTo(1);
                FocusCount++;
            }

            protected override void OnFocusLost(FocusLostEvent e)
            {
                base.OnFocusLost(e);
                Box.FadeTo(0.5f);
                FocusLostCount++;
            }

            public bool AllowBlockingKeyDown = true;

            // only KeyDown is blocking
            protected override bool OnKeyDown(KeyDownEvent e)
            {
                ++KeyDownCount;
                return AllowBlockingKeyDown;
            }

            protected override void OnKeyUp(KeyUpEvent e)
            {
                ++KeyUpCount;
                base.OnKeyUp(e);
            }

            protected override bool OnJoystickPress(JoystickPressEvent e)
            {
                ++JoystickPressCount;
                return base.OnJoystickPress(e);
            }

            protected override void OnJoystickRelease(JoystickReleaseEvent e)
            {
                ++JoystickReleaseCount;
                base.OnJoystickRelease(e);
            }
        }
    }
}
