// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using AppKit;
using Foundation;
using ObjCRuntime;
using osu.Framework.Platform.MacOS.Native;
using osu.Framework.Platform.SDL3;
using osuTK;

namespace osu.Framework.Platform.MacOS
{
    /// <summary>
    /// macOS-specific subclass of <see cref="SDL3Window"/>.
    /// </summary>
    internal class SDL3MacOSWindow : SDL3DesktopWindow
    {
        public static SDL3MacOSWindow SharedWindow;

        public SDL3MacOSWindow(GraphicsSurfaceType surfaceType, string appName)
            : base(surfaceType, appName)
        {
            if (SharedWindow != null)
                throw new InvalidOperationException("o!f does not support creating multiple windows on macOS.");

            SharedWindow = this;
        }

        internal void HandlePreciseScrollWheel(NSEvent theEvent)
        {
            // according to osuTK, 0.1f is the scaling factor expected to be returned by CGEventSourceGetPixelsPerLine
            // this is additionally scaled down by a factor of 8 so that a precise scroll of 1.0 is roughly equivalent to one notch on a traditional scroll wheel.
            const float scale_factor = 0.1f / 8;

            SharedWindow.ScheduleEvent(() =>
            {
                SharedWindow.TriggerMouseWheel(new Vector2(
                    (float)(theEvent.ScrollingDeltaX * scale_factor),
                    (float)(theEvent.ScrollingDeltaY * scale_factor)), true);
            });
        }
    }

    [Category(typeof(SDL3View))]
    internal static class SDL3ViewFrameworkOverrides
    {
        [Export("scrollWheel:")]
        public static void ScrollWheel(this SDL3View view, NSEvent theEvent)
        {
            if (theEvent.HasPreciseScrollingDeltas)
                SDL3MacOSWindow.SharedWindow.HandlePreciseScrollWheel(theEvent);
            else
                view.ScrollWheel(theEvent);
        }
    }
}
