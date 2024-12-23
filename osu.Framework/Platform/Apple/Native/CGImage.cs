// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Apple.Native
{
    internal readonly partial struct CGImage
    {
        internal IntPtr Handle { get; }

        public CGImage(IntPtr handle)
        {
            Handle = handle;
        }

        [LibraryImport(CGColorSpace.LIB_CORE_GRAPHICS, EntryPoint = "CGImageRelease")]
        public static partial void Release(CGImage image);
    }
}
