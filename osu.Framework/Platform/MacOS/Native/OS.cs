// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.MacOS.Native
{
    internal struct OS
    {
        internal const string OS_LOG_CATEGORY_POINTS_OF_INTEREST = @"PointsOfInterest";

        [DllImport(Cocoa.LIB_TRACE)]
        internal static extern OSLog os_log_create(string name, string category);

        [DllImport(Cocoa.LIB_TRACE)]
        internal static extern OSSignpostID os_signpost_id_generate(OSLog log);

        [DllImport("libSignposter.dylib")]
        internal static extern void signpost_interval_begin(OSLog log, OSSignpostID spid, string name);

        [DllImport("libSignposter.dylib")]
        internal static extern void signpost_interval_end(OSLog log, OSSignpostID spid, string name);

        [DllImport("libSignposter.dylib")]
        internal static extern void signpost_event_emit(OSLog log, OSSignpostID spid, string name);
    }
}
