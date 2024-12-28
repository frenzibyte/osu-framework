// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable InconsistentNaming

namespace osu.Framework.Platform.Apple.Native.Accelerate
{
    internal unsafe partial struct vImage_Buffer
    {
        public byte* Data;
        public nuint Height;
        public nuint Width;
        public nuint BytesPerRow;
    }
}
