// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using osu.Framework.Configuration;
using osu.Framework.Platform.MacOS.Native;
using osu.Framework.Platform.SDL3;
using osuTK;
using SDL;
using static SDL.SDL3;

namespace osu.Framework.Platform.MacOS
{
    /// <summary>
    /// macOS-specific subclass of <see cref="SDL3Window"/>.
    /// </summary>
    internal class SDL3MacOSWindow : SDL3DesktopWindow
    {
        private static readonly IntPtr sel_hasprecisescrollingdeltas = Selector.Get("hasPreciseScrollingDeltas");
        private static readonly IntPtr sel_scrollingdeltax = Selector.Get("scrollingDeltaX");
        private static readonly IntPtr sel_scrollingdeltay = Selector.Get("scrollingDeltaY");
        private static readonly IntPtr sel_respondstoselector_ = Selector.Get("respondsToSelector:");

        private delegate void ScrollWheelDelegate(IntPtr handle, IntPtr selector, IntPtr theEvent); // v@:@

        private IntPtr originalScrollWheel;
        private ScrollWheelDelegate scrollWheelHandler;

        public override IEnumerable<WindowMode> SupportedWindowModes => new[]
        {
            Configuration.WindowMode.Windowed,
            Configuration.WindowMode.Fullscreen,
        };

        public SDL3MacOSWindow(GraphicsSurfaceType surfaceType, string appName)
            : base(surfaceType, appName)
        {
        }

        public override void Create()
        {
            base.Create();

            // replace [SDLView scrollWheel:(NSEvent *)] with our own version
            IntPtr viewClass = Class.Get("SDL3View");
            scrollWheelHandler = scrollWheel;
            originalScrollWheel = Class.SwizzleMethod(viewClass, "scrollWheel:", "v@:@", scrollWheelHandler);
        }

        protected override unsafe Size SetFullscreen(SDL_DisplayMode sdlDisplayMode)
        {
            var desktopMode = SDL_GetDesktopDisplayMode(sdlDisplayMode.displayID);

            // SDL3 offers us two options for fullscreen:
            //   1. SDL3's "fullscreen" mode (resize window to fit current display)
            //   2. macOS's native "fullscreen space" mode (make window become fullscreen in a dedicated space for it)
            // the first option is good for its ability to change display mode in game,
            // but behaves finicky at times and gets broken by external window managers.
            // we'll go with the second option for now, by setting fullscreen mode to null.
            SDL_SetWindowFullscreenMode(SDLWindowHandle, null);
            SDL_SetWindowFullscreen(SDLWindowHandle, true);
            return Size.Round(new Size(desktopMode->w, desktopMode->h) * desktopMode->pixel_density);
        }

        /// <summary>
        /// Swizzled replacement of [SDLView scrollWheel:(NSEvent *)] that checks for precise scrolling deltas.
        /// </summary>
        private void scrollWheel(IntPtr receiver, IntPtr selector, IntPtr theEvent)
        {
            bool hasPrecise = Cocoa.SendBool(theEvent, sel_respondstoselector_, sel_hasprecisescrollingdeltas) &&
                              Cocoa.SendBool(theEvent, sel_hasprecisescrollingdeltas);

            if (!hasPrecise)
            {
                // calls the unswizzled [SDLView scrollWheel:(NSEvent *)] method if this is a regular scroll wheel event
                // the receiver may sometimes not be SDLView, ensure it has a scroll wheel selector implemented before attempting to call.
                if (Cocoa.SendBool(receiver, sel_respondstoselector_, originalScrollWheel))
                    Cocoa.SendVoid(receiver, originalScrollWheel, theEvent);

                return;
            }

            // according to osuTK, 0.1f is the scaling factor expected to be returned by CGEventSourceGetPixelsPerLine
            // this is additionally scaled down by a factor of 8 so that a precise scroll of 1.0 is roughly equivalent to one notch on a traditional scroll wheel.
            const float scale_factor = 0.1f / 8;

            float scrollingDeltaX = Cocoa.SendFloat(theEvent, sel_scrollingdeltax);
            float scrollingDeltaY = Cocoa.SendFloat(theEvent, sel_scrollingdeltay);

            ScheduleEvent(() => TriggerMouseWheel(new Vector2(scrollingDeltaX * scale_factor, scrollingDeltaY * scale_factor), true));
        }
    }
}
