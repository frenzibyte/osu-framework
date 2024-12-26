// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using AppKit;
using Foundation;
using ObjCRuntime;

namespace osu.Framework.Platform.MacOS.Native
{
    // todo: maybe move this to SDL2-CS but it's probably not worth the effort.
    [Register("SDLView")]
    internal class SDLView : NSView
    {
        protected SDLView(NativeHandle handle)
            : base(handle)
        {
        }
    }
}
