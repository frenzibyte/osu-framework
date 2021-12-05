// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using CoreGraphics;
using osu.Framework.Platform;
using UIKit;

namespace osu.Framework.iOS
{
    internal class GameViewController : UIViewController
    {
        private readonly IOSGameView gameView;
        private readonly GameHost gameHost;

        public override bool PrefersStatusBarHidden() => true;

        public override UIRectEdge PreferredScreenEdgesDeferringSystemGestures => UIRectEdge.All;

        public GameViewController(IOSGameView view, GameHost host)
        {
            View = view;

            gameView = view;
            gameHost = host;
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            gameHost.Collect();
        }

        public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
        {
            base.ViewWillTransitionToSize(toSize, coordinator);

            if (toSize == gameView.Bounds.Size)
                return;

            // expand the game view to the largest square of its pre-transition size,
            // so that the scaling of the gl viewport during transition doesn't look jarring.
            var bounds = gameView.Bounds;
            bounds.Width = NMath.Max(bounds.Width, bounds.Height);
            bounds.Height = NMath.Max(bounds.Width, bounds.Height);
            gameView.Bounds = bounds;

            coordinator.AnimateAlongsideTransition(_ => gameView.Bounds = new CGRect(CGPoint.Empty, toSize), null);
        }
    }
}
