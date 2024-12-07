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
    public class IOSDirectorySelector : UIDocumentPickerDelegate, ISystemDirectorySelector
    {
        public event Action<DirectoryInfo>? Selected;

        private readonly UIWindow window;
        private readonly UIDocumentPickerViewController viewController;

        public IOSDirectorySelector(UIWindow window)
        {
            this.window = window;

            viewController = new UIDocumentPickerViewController(new[] { UTTypes.Folder });
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
            => Selected?.Invoke(new DirectoryInfo(url.AbsoluteString!));

        protected override void Dispose(bool disposing)
        {
            var v = viewController;

            UIApplication.SharedApplication.InvokeOnMainThread(() => v.DismissViewController(true, null));
            viewController.Dispose();

            base.Dispose(disposing);
        }
    }
}
