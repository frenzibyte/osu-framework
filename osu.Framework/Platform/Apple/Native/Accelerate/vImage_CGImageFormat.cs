// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable InconsistentNaming

using CoreGraphics;
using ObjCRuntime;

namespace osu.Framework.Platform.Apple.Native.Accelerate
{
    internal unsafe struct vImage_CGImageFormat
    {
        public uint BitsPerComponent;
        public uint BitsPerPixel;
        public NativeHandle ColorSpace;
        public CGBitmapFlags BitmapInfo;
        public uint Version;
        public double* Decode;
        public CGColorRenderingIntent RenderingIntent;
    }
}
