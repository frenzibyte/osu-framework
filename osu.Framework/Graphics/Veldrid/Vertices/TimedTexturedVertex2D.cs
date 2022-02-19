// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osuTK;
using osuTK.Graphics;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TimedTexturedVertex2D : IEquatable<TimedTexturedVertex2D>, IVertex
    {
        [VertexMember(VertexElementFormat.Float2, VertexElementSemantic.Position)]
        public Vector2 Position;

        [VertexMember(VertexElementFormat.Float4, VertexElementSemantic.Color)]
        public Color4 Colour;

        [VertexMember(VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)]
        public Vector2 TexturePosition;

        [VertexMember(VertexElementFormat.Float1, VertexElementSemantic.Normal)]
        public float Time;

        public readonly bool Equals(TimedTexturedVertex2D other) => Position.Equals(other.Position) && TexturePosition.Equals(other.TexturePosition) && Colour.Equals(other.Colour) && Time.Equals(other.Time);
    }
}
