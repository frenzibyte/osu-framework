// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSWindow
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get("NSWindow");
        private static readonly IntPtr sel_display_link_with_target = Selector.Get("displayLinkWithTarget:selector:");

        internal NSWindow(IntPtr handle)
        {
            Handle = handle;
        }

        public CADisplayLink DisplayLinkWithTarget(IntPtr target, IntPtr selector)
            => new CADisplayLink(Cocoa.SendIntPtr(Handle, sel_display_link_with_target, target, selector));
    }
}
