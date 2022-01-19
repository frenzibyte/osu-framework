// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using CoreGraphics;
using Foundation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Logging;
using osu.Framework.Platform;
using UIKit;

namespace osu.Framework.iOS
{
    internal class GameViewController : UIViewController
    {
        private readonly IOSGameView gameView;
        private readonly GameHost gameHost;

        private readonly IBindable<ScreenOrientation> screenOrientation = new Bindable<ScreenOrientation>();
        private readonly IBindable<bool> lockScreenOrientation = new Bindable<bool>();

        public override UIRectEdge PreferredScreenEdgesDeferringSystemGestures => UIRectEdge.All;

        public override bool PrefersStatusBarHidden() => true;

        public GameViewController(IOSGameView view, GameHost host)
        {
            View = view;

            gameView = view;
            gameHost = host;

            var appDelegate = (GameAppDelegate)UIApplication.SharedApplication.Delegate;

            appDelegate.HostStarted += () =>
            {
                screenOrientation.BindTo(gameHost.ScreenOrientation);
                lockScreenOrientation.BindTo(gameHost.LockScreenOrientation);

                screenOrientation.BindValueChanged(_ => updateDeviceOrientation());
                lockScreenOrientation.BindValueChanged(_ => updateDeviceOrientation(), true);

                // Updating the device orientation on start does not call ViewWillTransitionToSize,
                // but we always need that to resize the game framebuffer.
                // Therefore do it manually here for now.
                gameView.RequestResizeFrameBuffer();
            };
        }

        private void updateDeviceOrientation()
        {
            if (lockScreenOrientation.Value)
                return;

            switch (screenOrientation.Value)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.AnyPortrait when !UIDevice.CurrentDevice.Orientation.IsPortrait():
                    UIDevice.CurrentDevice.SetValueForKey(NSNumber.FromInt32((int)UIDeviceOrientation.Portrait), (NSString)"orientation");
                    break;

                case ScreenOrientation.ReversePortrait:
                    UIDevice.CurrentDevice.SetValueForKey(NSNumber.FromInt32((int)UIDeviceOrientation.PortraitUpsideDown), (NSString)"orientation");
                    break;

                case ScreenOrientation.LandscapeLeft:
                case ScreenOrientation.AnyLandscape when !UIDevice.CurrentDevice.Orientation.IsLandscape():
                    UIDevice.CurrentDevice.SetValueForKey(NSNumber.FromInt32((int)UIDeviceOrientation.LandscapeLeft), (NSString)"orientation");
                    break;

                case ScreenOrientation.LandscapeRight:
                    UIDevice.CurrentDevice.SetValueForKey(NSNumber.FromInt32((int)UIDeviceOrientation.LandscapeRight), (NSString)"orientation");
                    break;

                case ScreenOrientation.Any:
                case ScreenOrientation.Auto:
                    break;
            }

            AttemptRotationToDeviceOrientation();
        }

        public override bool ShouldAutorotate()
        {
            if (lockScreenOrientation.Value)
                return false;

            switch (screenOrientation.Value)
            {
                default:
                case ScreenOrientation.Any:
                case ScreenOrientation.Auto:
                    return true;

                case ScreenOrientation.Portrait:
                    return UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.Portrait;

                case ScreenOrientation.ReversePortrait:
                    return UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.PortraitUpsideDown;

                case ScreenOrientation.AnyPortrait:
                    return UIDevice.CurrentDevice.Orientation.IsPortrait();

                case ScreenOrientation.LandscapeLeft:
                    return UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeLeft;

                case ScreenOrientation.LandscapeRight:
                    return UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeRight;

                case ScreenOrientation.AnyLandscape:
                    return UIDevice.CurrentDevice.Orientation.IsLandscape();
            }
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            gameHost.Collect();
        }

        public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
        {
            coordinator.AnimateAlongsideTransition(_ => { }, _ => UIView.AnimationsEnabled = true);
            UIView.AnimationsEnabled = false;

            base.ViewWillTransitionToSize(toSize, coordinator);
            gameView.RequestResizeFrameBuffer();
        }
    }
}
