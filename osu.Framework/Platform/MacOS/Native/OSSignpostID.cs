// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform.MacOS.Native
{
    internal enum OSSignpostType : byte
    {
        Event = 0x00,
        BeginInterval = 0x01,
        EndInterval = 0x02,
    }

    internal readonly record struct OSSignpostID
    {
        internal static readonly OSSignpostID NULL = new OSSignpostID(0x0000000000000000);
        internal static readonly OSSignpostID INVALID = new OSSignpostID(0xFFFFFFFFFFFFFFFF);
        internal static readonly OSSignpostID EXCLUSIVE = new OSSignpostID(0xEEEEB0B5B2B2EEEE);

        internal readonly ulong ID;

        internal OSSignpostID(ulong id)
        {
            ID = id;
        }
    }
}
