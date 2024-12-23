// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Apple.Native
{
    public readonly partial struct CGColorSpace
    {
        internal const string LIB_CORE_GRAPHICS = "/System/Library/Frameworks/CoreGraphics.framework/Versions/Current/CoreGraphics";

        internal IntPtr Handle { get; }

        internal CGColorSpace(IntPtr handle)
        {
            Handle = handle;
        }

        [LibraryImport(LIB_CORE_GRAPHICS, EntryPoint = "CGColorSpaceCreateDeviceRGB")]
        internal static partial CGColorSpace CreateDeviceRGB();
    }
}
