// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using AppKit;
using Foundation;
using osu.Framework.IO.Stores;
using osu.Framework.Platform.Apple;
using SixLabors.ImageSharp;

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
            uint length = (uint)(stream.Length - stream.Position);
            using var nativeData = NSMutableData.FromLength((int)length);

            var bytesSpan = new Span<byte>(nativeData.MutableBytes.ToPointer(), (int)length);
            stream.ReadExactly(bytesSpan);

            using var nsImage = new NSImage(nativeData);

            if (nsImage.Handle == IntPtr.Zero)
                throw new ArgumentException($"{nameof(Image)} could not be created from {nameof(stream)}.");

            var cgImage = nsImage.CGImage;
            return ImageFromCGImage<TPixel>(cgImage);
        }
    }
}
