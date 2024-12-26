// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using AppKit;
using ObjCRuntime;
using SixLabors.ImageSharp;
using NSObject = Foundation.NSObject;
using NSString = Foundation.NSString;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSClipboard : Clipboard
    {
        public override string? GetText() => getFromPasteboard<NSString>();

        public override Image<TPixel>? GetImage<TPixel>()
        {
            using var nsImage = getFromPasteboard<NSImage>();
            using var nsData = nsImage?.AsTiff();

            if (nsData == null)
                return null;

            return Image.Load<TPixel>(nsData.ToArray());
        }

        public override void SetText(string text) => setToPasteboard(new NSString(text));

        public override bool SetImage(Image image)
        {
            using (var stream = new MemoryStream())
            {
                image.SaveAsTiff(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var nsImage = NSImage.FromStream(stream);
                return setToPasteboard(nsImage);
            }
        }

        private T? getFromPasteboard<T>()
            where T : NSObject, INSPasteboardWriting
        {
            T? result = null;

            NSApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                Class[] classArray = new[] { new Class(typeof(T)) };

                if (!NSPasteboard.GeneralPasteboard.CanReadObjectForClasses(classArray, null))
                {
                    result = null;
                    return;
                }

                var objects = NSPasteboard.GeneralPasteboard.ReadObjectsForClasses(classArray, null);
                result = objects.Length == 0 ? null : (T)objects[0];
            });

            return result;
        }

        private bool setToPasteboard<T>(T? value)
            where T : NSObject, INSPasteboardWriting
        {
            if (value == null)
                return false;

            NSApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                NSPasteboard.GeneralPasteboard.ClearContents();
                NSPasteboard.GeneralPasteboard.WriteObjects(new INSPasteboardWriting[] { value });
            });

            return true;
        }
    }
}
