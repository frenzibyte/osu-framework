﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Process = System.Diagnostics.Process;
using NativeOrientation = Android.Content.PM.ScreenOrientation;
using ConfigOrientation = osu.Framework.Configuration.ScreenOrientation;

namespace osu.Framework.Android
{
    public abstract class AndroidGameActivity : Activity
    {
        protected abstract Game CreateGame();

        /// <summary>
        /// The visibility flags for the system UI (status and navigation bars)
        /// </summary>
        public SystemUiFlags UIVisibilityFlags
        {
            get => (SystemUiFlags)Window.DecorView.SystemUiVisibility;
            set
            {
                systemUiFlags = value;
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)value;
            }
        }

        private SystemUiFlags systemUiFlags;

        private AndroidGameView gameView;

        public override void OnTrimMemory([GeneratedEnum] TrimMemory level)
        {
            base.OnTrimMemory(level);
            gameView.Host?.Collect();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(gameView = new AndroidGameView(this, CreateGame()));

            UIVisibilityFlags = SystemUiFlags.LayoutFlags | SystemUiFlags.ImmersiveSticky | SystemUiFlags.HideNavigation;

            // Firing up the on-screen keyboard (eg: interacting with textboxes) may cause the UI visibility flags to be altered thus showing the navigation bar and potentially the status bar
            // This sets back the UI flags to hidden once the interaction with the on-screen keyboard has finished.
            Window.DecorView.SystemUiVisibilityChange += (_, e) =>
            {
                if ((SystemUiFlags)e.Visibility != systemUiFlags)
                {
                    UIVisibilityFlags = systemUiFlags;
                }
            };

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;

            gameView.HostStarted += host =>
            {
                host.AllowScreenSuspension.Result.BindValueChanged(allow =>
                {
                    RunOnUiThread(() =>
                    {
                        if (!allow.NewValue)
                            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                        else
                            Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
                    });
                }, true);

                host.ScreenOrientation.BindValueChanged(e =>
                {
                    // Don't do anything if orientation is locked
                    if (host.LockScreenOrientation.Value) return;

                    RunOnUiThread(() =>
                    {
                        RequestedOrientation = configToNativeOrientationEnum(e.NewValue);
                    });
                });
                host.LockScreenOrientation.BindValueChanged(e =>
                {
                    // Throw if consumer change locked state while ScreenOrientation bindable is disabled
                    if (host.ScreenOrientation.Disabled)
                        throw new InvalidOperationException("Can't change screen orientation lock when setting is disabled");

                    if (e.NewValue) // If locked
                    {
                        RunOnUiThread(() =>
                        {
                            RequestedOrientation = NativeOrientation.Locked;
                        });
                    }
                    else host.ScreenOrientation.TriggerChange();
                });
            };
        }

        protected override void OnPause()
        {
            base.OnPause();
            // Because Android is not playing nice with Background - we just kill it
            Process.GetCurrentProcess().Kill();
        }

        public override void OnBackPressed()
        {
            // Avoid the default implementation that does close the app.
            // This only happens when the back button could not be captured from OnKeyDown.
        }

        // On some devices and keyboard combinations the OnKeyDown event does not propagate the key event to the view.
        // Here it is done manually to ensure that the keys actually land in the view.

        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyUp(keyCode, e);
        }

        public override bool OnKeyLongPress([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyLongPress(keyCode, e);
        }

        /// <summary>
        /// Convert screen orientation framework config enum to equivalent Android's native orientation enum
        /// </summary>
        /// <param name="orientation">Framework setting enum to convert</param>
        /// <returns><see cref="NativeOrientation"/> enum to use with Android SDK</returns>
        private static NativeOrientation configToNativeOrientationEnum(ConfigOrientation orientation)
        {
            switch (orientation)
            {
                case ConfigOrientation.AnyLandscape:
                    return NativeOrientation.SensorLandscape;

                case ConfigOrientation.AnyPortrait:
                    return NativeOrientation.SensorPortrait;

                case ConfigOrientation.LandscapeLeft:
                    return NativeOrientation.ReverseLandscape;

                case ConfigOrientation.LandscapeRight:
                    return NativeOrientation.Landscape;

                case ConfigOrientation.Portrait:
                    return NativeOrientation.Portrait;

                case ConfigOrientation.ReversePortrait:
                    return NativeOrientation.ReversePortrait;

                case ConfigOrientation.Any:
                    return NativeOrientation.FullSensor;

                case ConfigOrientation.Auto:
                    return NativeOrientation.FullUser;

                default:
                    throw new ArgumentOutOfRangeException(nameof(orientation), "Unknown framework config ScreenOrientation enum member");
            }
        }
    }
}
