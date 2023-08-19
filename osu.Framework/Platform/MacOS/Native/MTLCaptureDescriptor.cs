// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct MTLCaptureDescriptor
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get(nameof(MTLCaptureDescriptor));
        private static readonly IntPtr sel_capture_object = Selector.Get("captureObject");
        private static readonly IntPtr sel_set_capture_object = Selector.Get("setCaptureObject:");
        private static readonly IntPtr sel_destination = Selector.Get("destination");
        private static readonly IntPtr sel_set_destination = Selector.Get("setDestination:");
        private static readonly IntPtr sel_output_url = Selector.Get("outputURL");
        private static readonly IntPtr sel_set_output_url = Selector.Get("setOutputURL:");

        public MTLCaptureDescriptor(IntPtr handle)
        {
            Handle = handle;
        }

        internal static MTLCaptureDescriptor Init()
        {
            IntPtr descriptor = Cocoa.SendIntPtr(class_pointer, Selector.Get("alloc"));
            Cocoa.SendVoid(descriptor, Selector.Get("init"));
            return new MTLCaptureDescriptor(descriptor);
        }

        internal IntPtr CaptureObject
        {
            get => Cocoa.SendIntPtr(Handle, sel_capture_object);
            set => Cocoa.SendVoid(Handle, sel_set_capture_object, value);
        }

        internal MTLCaptureDestination Destination
        {
            get => (MTLCaptureDestination)Cocoa.SendInt(Handle, sel_destination);
            set => Cocoa.SendVoid(Handle, sel_set_destination, (int)value);
        }

        internal IntPtr OutputUrl
        {
            get => Cocoa.SendIntPtr(Handle, sel_output_url);
            set => Cocoa.SendVoid(Handle, sel_set_output_url, value);
        }
    }
}
