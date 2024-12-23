// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform.Apple.Native;
using osu.Framework.Platform.MacOS.Native;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSTextureLoaderStore : TextureLoaderStore
    {
        public MacOSTextureLoaderStore(IResourceStore<byte[]> store)
            : base(store)
        {
            if (!stopwatch.IsRunning)
                stopwatch.Start();
        }

        private static readonly Stopwatch stopwatch = new Stopwatch();

        protected override unsafe Image<TPixel> ImageFromStream<TPixel>(Stream stream)
        {
            double time = stopwatch.ElapsedMilliseconds;

            var pool = NSAutoreleasePool.Init();

            var nativeData = NSData.FromBytes(stream.ReadAllBytesToArray());
            if (nativeData.Handle == IntPtr.Zero)
                throw new ArgumentException($"{nameof(Image)} could not be created from {nameof(stream)}.");

            var nsImage = NSImage.InitWithData(nativeData);
            var nsImageRep = new NSImageRep(nsImage.Representations().ObjectAtIndex(0));

            int width = nsImageRep.PixelsWide;
            int height = nsImageRep.PixelsHigh;

            var cgImage = nsImageRep.CGImage();

            var format = new Accelerate.vImage_CGImageFormat
            {
                BitsPerComponent = 8,
                BitsPerPixel = 32,
                ColorSpace = CGColorSpace.CreateDeviceRGB(),
                // notably, iOS generally uses premultiplied alpha when rendering image to pixels via CGBitmapContext or otherwise,
                // but vImage offers using straight alpha directly without any conversion from our side (by specifying Last instead of PremultipliedLast).
                BitmapInfo = (CGBitmapFlags)CGImageAlphaInfo.Last,
                Decode = null,
                RenderingIntent = CGColorRenderingIntent.Default,
            };

            Accelerate.vImageBuffer accelerateImage = default;

            // perform initial call to retrieve preferred alignment and bytes-per-row values for the given image dimensions.
            nuint alignment = (nuint)Accelerate.vImageBuffer_Init(&accelerateImage, (uint)height, (uint)width, 32, Accelerate.vImageFlags.NoAllocate);
            Debug.Assert(alignment > 0);

            // allocate aligned memory region to contain image pixel data.
            nuint bytesPerRow = accelerateImage.BytesPerRow;
            nuint bytesCount = bytesPerRow * accelerateImage.Height;
            accelerateImage.Data = (byte*)NativeMemory.AlignedAlloc(bytesCount, alignment);

            var result = Accelerate.vImageBuffer_InitWithCGImage(&accelerateImage, &format, null, cgImage.Handle, Accelerate.vImageFlags.NoAllocate);
            Debug.Assert(result == Accelerate.vImageError.NoError);

            var image = new Image<TPixel>(width, height);

            for (int i = 0; i < height; i++)
            {
                var imageRow = image.DangerousGetPixelRowMemory(i);
                var dataRow = new ReadOnlySpan<TPixel>(&accelerateImage.Data[(int)bytesPerRow * i], width);
                dataRow.CopyTo(imageRow.Span);
            }

            NativeMemory.AlignedFree(accelerateImage.Data);

            CGImage.Release(cgImage);
            nsImage.Release();
            nativeData.Release();
            pool.Release();

            Logger.Log($"MacOSTextureLoaderStore: texture loading spent {stopwatch.ElapsedMilliseconds - time:0.00}ms", "image", LogLevel.Important);
            return image;
        }
    }
}
