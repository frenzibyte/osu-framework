// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Runtime.Versioning;
using Foundation;
using osu.Framework.Platform;
using UIKit;
using UniformTypeIdentifiers;

namespace osu.Framework.iOS
{
    [SupportedOSPlatform("ios14.0")]
    public class IOSFileSelector : UIDocumentPickerDelegate, ISystemFileSelector
    {
        public event Action<FileInfo>? Selected;

        private readonly UIWindow window;

        private readonly UIDocumentPickerViewController viewController;

        public IOSFileSelector(UIWindow window, string[] allowedExtensions)
        {
            this.window = window;

            UTType[] utTypes;

            if (allowedExtensions.Length == 0)
                utTypes = new[] { UTTypes.Data };
            else
            {
                utTypes = new UTType[allowedExtensions.Length];

                for (int i = 0; i < allowedExtensions.Length; i++)
                {
                    string extension = allowedExtensions[i];

                    var type = UTType.CreateFromExtension(extension.Replace(".", string.Empty));

                    if (type == null)
                    {
                        // todo: fix messsage lol
                        throw new InvalidOperationException($"System failed to recognise extension \"{extension}\" when creating file selector.\n"
                                                            + $"If this is an extension provided by your application, consider adding it to whatever.");
                    }

                    utTypes[i] = type;
                }
            }

            viewController = new UIDocumentPickerViewController(utTypes, true);
            viewController.Delegate = this;
        }

        public void Present()
        {
            UIApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                window.RootViewController!.PresentViewController(viewController, true, null);
            });
        }

        public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url)
            => Selected?.Invoke(new FileInfo(url.Path!));

        protected override void Dispose(bool disposing)
        {
            viewController.Dispose();
            base.Dispose(disposing);
        }
    }
}
