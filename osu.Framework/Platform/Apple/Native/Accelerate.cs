// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace osu.Framework.Platform.Apple.Native
{
    internal partial class Accelerate
    {
        private const string library = "/System/Library/Frameworks/Accelerate.framework/Accelerate";

        [LibraryImport(library)]
        internal static unsafe partial vImageError vImageBuffer_Init(vImageBuffer* buf, uint height, uint width, uint pixelBits, vImageFlags flags);

        [LibraryImport(library)]
        internal static unsafe partial vImageError vImageBuffer_InitWithCGImage(vImageBuffer* buf, vImage_CGImageFormat* format, double* backgroundColour, IntPtr image, vImageFlags flags);

        public enum vImageError : long
        {
            OutOfPlaceOperationRequired = -21780, // 0xFFFFFFFFFFFFAAEC
            ColorSyncIsAbsent = -21779, // 0xFFFFFFFFFFFFAAED
            InvalidImageFormat = -21778, // 0xFFFFFFFFFFFFAAEE
            InvalidRowBytes = -21777, // 0xFFFFFFFFFFFFAAEF
            InternalError = -21776, // 0xFFFFFFFFFFFFAAF0
            UnknownFlagsBit = -21775, // 0xFFFFFFFFFFFFAAF1
            BufferSizeMismatch = -21774, // 0xFFFFFFFFFFFFAAF2
            InvalidParameter = -21773, // 0xFFFFFFFFFFFFAAF3
            NullPointerArgument = -21772, // 0xFFFFFFFFFFFFAAF4
            MemoryAllocationError = -21771, // 0xFFFFFFFFFFFFAAF5
            InvalidOffsetY = -21770, // 0xFFFFFFFFFFFFAAF6
            InvalidOffsetX = -21769, // 0xFFFFFFFFFFFFAAF7
            InvalidEdgeStyle = -21768, // 0xFFFFFFFFFFFFAAF8
            InvalidKernelSize = -21767, // 0xFFFFFFFFFFFFAAF9
            RoiLargerThanInputBuffer = -21766, // 0xFFFFFFFFFFFFAAFA
            NoError = 0,
        }

        public enum vImageFlags : uint
        {
            NoFlags = 0,
            LeaveAlphaUnchanged = 1,
            CopyInPlace = 2,
            BackgroundColorFill = 4,
            EdgeExtend = 8,
            DoNotTile = 16, // 0x00000010
            HighQualityResampling = 32, // 0x00000020
            TruncateKernel = 64, // 0x00000040
            GetTempBufferSize = 128, // 0x00000080
            PrintDiagnosticsToConsole = 256, // 0x00000100
            NoAllocate = 512, // 0x00000200
        }

        public unsafe struct vImageBuffer
        {
            public byte* Data;
            public nuint Height;
            public nuint Width;
            public nuint BytesPerRow;
        }

        public unsafe struct vImage_CGImageFormat
        {
            public uint BitsPerComponent;
            public uint BitsPerPixel;
            public CGColorSpace ColorSpace;
            public CGBitmapFlags BitmapInfo;
            public uint Version;
            public double* Decode;
            public CGColorRenderingIntent RenderingIntent;
        }
    }
}
