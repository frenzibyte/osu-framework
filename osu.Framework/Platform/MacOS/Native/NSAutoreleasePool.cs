// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSAutoreleasePool
    {
        internal IntPtr Handle { get; }

        internal NSAutoreleasePool(IntPtr handle)
        {
            Handle = handle;
        }

        private static readonly IntPtr class_pointer = Class.Get("NSImage");
        private static readonly IntPtr sel_alloc = Selector.Get("alloc");
        private static readonly IntPtr sel_init = Selector.Get("init");
        private static readonly IntPtr sel_release = Selector.Get("release");

        internal static NSAutoreleasePool Init()
        {
            var pool = alloc();
            return new NSAutoreleasePool(Cocoa.SendIntPtr(pool.Handle, sel_init));
        }

        internal void Release() => Cocoa.SendVoid(Handle, sel_release);

        private static NSAutoreleasePool alloc() => new NSAutoreleasePool(Cocoa.SendIntPtr(class_pointer, sel_alloc));
    }
}
