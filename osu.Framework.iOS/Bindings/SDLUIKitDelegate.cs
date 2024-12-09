// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL.iOSBindings;
using UIKit;

namespace osu.Framework.iOS.Bindings
{
    public class GameAppDelegate : SDLUIKitDelegate
    {
        public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, UIWindow forWindow)
        {
            return base.GetSupportedInterfaceOrientations(application, forWindow);
        }
    }
}
