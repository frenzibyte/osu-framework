// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Foundation;
using ObjCRuntime;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Framework.Platform;
using Photos;
using PhotosUI;
using UIKit;
using UniformTypeIdentifiers;

namespace osu.Framework.iOS
{
    [SupportedOSPlatform("ios14.0")]
    public class IOSImageSelector : PHPickerViewControllerDelegate, ISystemFileSelector
    {
        private readonly UIWindow window;
        private readonly UTType[] types;
        public event Action<FileInfo>? Selected;
        public event Action? Cancelled;

        private PHPickerViewController viewController;

        public IOSImageSelector(UIWindow window, UTType[] types)
        {
            this.window = window;
            this.types = types;

            viewController = new PHPickerViewController(new PHPickerConfiguration(PHPhotoLibrary.SharedPhotoLibrary)
            {
                Filter = PHPickerFilter.ImagesFilter,
                PreferredAssetRepresentationMode = PHPickerConfigurationAssetRepresentationMode.Compatible,
            });
            viewController.Delegate = this;
        }

        public void Present()
        {
            UIApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                window.RootViewController!.PresentViewController(viewController, true, null);
            });
        }

        public override void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results)
        {
            picker.DismissViewController(true, null);

            if (results.Length == 0)
                return;

            var result = results[0];

            string[] availableTypes = result.ItemProvider.RegisteredTypeIdentifiers;
            string? targetType = availableTypes.FirstOrDefault(at => types.Any(t => t.Identifier == at));

            if (targetType == null)
            {
                Logger.Error(null, "Selected image does not have a valid representable type.");
                return;
            }

            result.ItemProvider.LoadFileRepresentation(targetType, (url, error) => Selected?.Invoke(new FileInfo(url.Path!)));
        }
    }
}
