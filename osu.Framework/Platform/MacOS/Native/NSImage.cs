// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSImage
    {
        internal IntPtr Handle { get; }

        internal NSImage(IntPtr handle)
        {
            Handle = handle;
        }

        private static readonly IntPtr class_pointer = Class.Get("NSImage");
        private static readonly IntPtr sel_alloc = Selector.Get("alloc");
        private static readonly IntPtr sel_release = Selector.Get("release");
        private static readonly IntPtr sel_init_with_data = Selector.Get("initWithData:");
        private static readonly IntPtr sel_representations = Selector.Get("representations");

        internal NSArray Representations() => new NSArray(Cocoa.SendIntPtr(Handle, sel_representations));

        internal static NSImage InitWithData(NSData data)
        {
            var image = alloc();
            return new NSImage(Cocoa.SendIntPtr(image.Handle, sel_init_with_data, data));
        }

        internal void Release() => Cocoa.SendVoid(Handle, sel_release);

        private static NSImage alloc() => new NSImage(Cocoa.SendIntPtr(class_pointer, sel_alloc));
    }
}
