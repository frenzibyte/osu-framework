// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSRunLoop
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get("NSRunLoop");
        private static readonly IntPtr sel_current_run_loop = Selector.Get("currentRunLoop");
        private static readonly IntPtr sel_main_run_loop = Selector.Get("mainRunLoop");
        private static readonly IntPtr sel_run = Selector.Get("run");
        private static readonly IntPtr sel_run_until_date = Selector.Get("runUntilDate:");

        public static NSRunLoop CurrentRunLoop { get; } = new NSRunLoop(Cocoa.SendIntPtr(class_pointer, sel_current_run_loop));
        public static NSRunLoop MainRunLoop { get; } = new NSRunLoop(Cocoa.SendIntPtr(class_pointer, sel_main_run_loop));

        internal NSRunLoop(IntPtr handle)
        {
            Handle = handle;
        }

        public void Run()
            => Cocoa.SendVoid(Handle, sel_run);

        public bool Run(NSDate date)
            => Cocoa.SendBool(Handle, sel_run_until_date, date.Handle);
    }
}
