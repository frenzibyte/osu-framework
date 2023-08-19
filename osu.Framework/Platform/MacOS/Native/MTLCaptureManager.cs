// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct MTLCaptureManager
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get(nameof(MTLCaptureManager));
        private static readonly IntPtr sel_shared_capture_manager = Selector.Get("sharedCaptureManager");
        private static readonly IntPtr sel_start_capture_with_descriptor = Selector.Get("startCaptureWithDescriptor:error:");
        private static readonly IntPtr sel_stop_capture = Selector.Get("stopCapture");

        internal MTLCaptureManager(IntPtr handle)
        {
            Handle = handle;
        }

        internal static MTLCaptureManager SharedCaptureManager => new MTLCaptureManager(Cocoa.SendIntPtr(class_pointer, sel_shared_capture_manager));

        internal bool StartCapture(IntPtr descriptor) => Cocoa.SendBool(Handle, sel_start_capture_with_descriptor, descriptor, IntPtr.Zero);

        internal void StopCapture() => Cocoa.SendVoid(Handle, sel_stop_capture);
    }
}
