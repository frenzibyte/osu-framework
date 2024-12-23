// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform.Apple.Native
{
    public enum CGBitmapFlags : uint
    {
        None = 0,
        PremultipliedLast = 1,
        PremultipliedFirst = 2,
        Last = PremultipliedFirst | PremultipliedLast, // 0x00000003
        First = 4,
        NoneSkipLast = First | PremultipliedLast, // 0x00000005
        NoneSkipFirst = First | PremultipliedFirst, // 0x00000006
        Only = NoneSkipFirst | PremultipliedLast, // 0x00000007
        AlphaInfoMask = 31, // 0x0000001F
        FloatInfoMask = 3840, // 0x00000F00
        FloatComponents = 256, // 0x00000100
        ByteOrderMask = 28672, // 0x00007000
        ByteOrderDefault = 0,
        ByteOrder16Little = 4096, // 0x00001000
        ByteOrder32Little = 8192, // 0x00002000
        ByteOrder16Big = ByteOrder32Little | ByteOrder16Little, // 0x00003000
        ByteOrder32Big = 16384, // 0x00004000
    }
}
