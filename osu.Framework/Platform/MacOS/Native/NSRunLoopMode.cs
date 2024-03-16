// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSRunLoopMode
    {
        internal IntPtr Handle { get; }

        internal static NSRunLoopMode NSRunLoopCommonModes { get; } = new NSRunLoopMode(Cocoa.GetStringConstant(Cocoa.AppKitLibrary, nameof(NSRunLoopCommonModes)));
        internal static NSRunLoopMode NSDefaultRunLoopMode { get; } = new NSRunLoopMode(Cocoa.GetStringConstant(Cocoa.AppKitLibrary, nameof(NSDefaultRunLoopMode)));
        internal static NSRunLoopMode NSEventTrackingRunLoopMode { get; } = new NSRunLoopMode(Cocoa.GetStringConstant(Cocoa.AppKitLibrary, nameof(NSEventTrackingRunLoopMode)));
        internal static NSRunLoopMode NSModalPanelRunLoopMode { get; } = new NSRunLoopMode(Cocoa.GetStringConstant(Cocoa.AppKitLibrary, nameof(NSModalPanelRunLoopMode)));
        internal static NSRunLoopMode UITrackingRunLoopMode { get; } = new NSRunLoopMode(Cocoa.GetStringConstant(Cocoa.AppKitLibrary, nameof(UITrackingRunLoopMode)));

        public NSRunLoopMode(IntPtr handle)
        {
            Handle = handle;
        }
    }
}
