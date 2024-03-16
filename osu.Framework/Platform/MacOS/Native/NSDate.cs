// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSDate
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get("NSDate");
        private static readonly IntPtr sel_now = Selector.Get("now");
        private static readonly IntPtr sel_distant_future = Selector.Get("distantFuture");
        private static readonly IntPtr sel_date_by_adding_time_interval = Selector.Get("dateByAddingTimeInterval:");

        public static NSDate Now => new NSDate(Cocoa.SendIntPtr(class_pointer, sel_now));
        public static NSDate DistantFuture => new NSDate(Cocoa.SendIntPtr(class_pointer, sel_distant_future));

        internal NSDate(IntPtr handle)
        {
            Handle = handle;
        }

        public NSDate DateByAddingTimeInterval(double interval)
            => new NSDate(Cocoa.SendIntPtr(Handle, sel_date_by_adding_time_interval, interval));
    }
}
