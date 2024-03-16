// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct CADisplayLink
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get("CADisplayLink");
        private static readonly IntPtr sel_display_link_with_target = Selector.Get("displayLinkWithTarget:selector:");
        private static readonly IntPtr sel_preferred_frame_rate_range = Selector.Get("preferredFrameRateRange:");
        private static readonly IntPtr sel_set_preferred_frame_rate_range = Selector.Get("setPreferredFrameRateRange:");
        private static readonly IntPtr sel_add_to_run_loop = Selector.Get("addToRunLoop:forMode:");

        public CAFrameRateRange PreferredFrameRateRange
        {
            get => Cocoa.SendCAFrameRateRange(Handle, sel_preferred_frame_rate_range);
            set => Cocoa.SendVoid(Handle, sel_set_preferred_frame_rate_range, value);
        }

        internal CADisplayLink(IntPtr handle)
        {
            Handle = handle;
        }

        public static CADisplayLink DisplayLinkWithTarget(IntPtr target, IntPtr selector)
            => new CADisplayLink(Cocoa.SendIntPtr(class_pointer, sel_display_link_with_target, target, selector));

        public void AddToRunLoop(NSRunLoop runLoop, NSRunLoopMode mode) => Cocoa.SendVoid(Handle, sel_add_to_run_loop, runLoop.Handle, mode.Handle);
    }
}
