// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TimedTexturedVertex2D : IEquatable<TimedTexturedVertex2D>, IVertex
    {
        public Vector2 Position;

        public Color4 Colour;

        public Vector2 TexturePosition;

        public float Time;

        public readonly bool Equals(TimedTexturedVertex2D other)
            => Position.Equals(other.Position)
               && TexturePosition.Equals(other.TexturePosition)
               && Colour.Equals(other.Colour)
               && Time.Equals(other.Time);
    }
}
