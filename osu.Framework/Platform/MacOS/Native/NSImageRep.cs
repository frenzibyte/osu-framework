// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform.Apple.Native;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSImageRep
    {
        internal IntPtr Handle { get; }

        internal NSImageRep(IntPtr handle)
        {
            Handle = handle;
        }

        private static readonly IntPtr sel_pixels_wide = Selector.Get("pixelsWide");
        private static readonly IntPtr sel_pixels_high = Selector.Get("pixelsHigh");
        private static readonly IntPtr sel_cg_image_for_proposed_rect = Selector.Get("CGImageForProposedRect:context:hints:");

        internal int PixelsWide => Cocoa.SendInt(Handle, sel_pixels_wide);
        internal int PixelsHigh => Cocoa.SendInt(Handle, sel_pixels_high);

        internal CGImage CGImage() => new CGImage(Cocoa.SendIntPtr(Handle, sel_cg_image_for_proposed_rect, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero));
    }
}
