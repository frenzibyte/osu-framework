// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using AppKit;
using Foundation;
using osu.Framework.IO.Stores;
using osu.Framework.Platform.Apple;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;

namespace osu.Framework.Platform.MacOS
{
    internal class MacOSTextureLoaderStore : AppleTextureLoaderStore
    {
        public MacOSTextureLoaderStore(IResourceStore<byte[]> store)
            : base(store)
        {
        }

        protected override unsafe Image<TPixel> ImageFromStream<TPixel>(Stream stream)
        {
            int length = (int)(stream.Length - stream.Position);
            using var buffer = MemoryAllocator.Default.Allocate<byte>(length);
            stream.ReadExactly(buffer.Memory.Span);

            fixed (byte* ptr = buffer.Memory.Span)
            {
                using var nativeData = NSData.FromBytesNoCopy((IntPtr)ptr, (nuint)length, false);
                using var nsImage = new NSImage(nativeData);

                if (nsImage.Handle == IntPtr.Zero)
                    throw new ArgumentException($"{nameof(Image)} could not be created from {nameof(stream)}.");

                var cgImage = nsImage.CGImage;
                return ImageFromCGImage<TPixel>(cgImage);
            }
        }
    }
}
