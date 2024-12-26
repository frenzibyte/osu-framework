// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using AppKit;
using Foundation;
using ObjCRuntime;

namespace osu.Framework.Platform.MacOS.Native
{
    // todo: move this to SDL3-CS
    [Register("SDL3View")]
    internal class SDL3View : NSView
    {
        protected SDL3View(NativeHandle handle)
            : base(handle)
        {
        }
    }
}
