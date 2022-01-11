// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TexturedVertex2D : IEquatable<TexturedVertex2D>, IVertex
    {
        public Vector2 Position;

        public Color4 Colour;

        public Vector2 TexturePosition;

        public Vector4 TextureRect;

        public Vector2 BlendRange;

        public readonly bool Equals(TexturedVertex2D other) =>
            Position.Equals(other.Position)
            && TexturePosition.Equals(other.TexturePosition)
            && Colour.Equals(other.Colour)
            && TextureRect.Equals(other.TextureRect)
            && BlendRange.Equals(other.BlendRange);
    }
}
