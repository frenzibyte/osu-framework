// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osuTK;
using osuTK.Graphics;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TexturedVertex2D : IEquatable<TexturedVertex2D>, IVertex
    {
        [VertexLayoutElement(VertexElementFormat.Float2, VertexElementSemantic.Position)]
        public Vector2 Position;

        [VertexLayoutElement(VertexElementFormat.Float4, VertexElementSemantic.Color)]
        public Color4 Colour;

        [VertexLayoutElement(VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)]
        public Vector2 TexturePosition;

        [VertexLayoutElement(VertexElementFormat.Float4, VertexElementSemantic.Normal)]
        public Vector4 TextureRect;

        [VertexLayoutElement(VertexElementFormat.Float2, VertexElementSemantic.Normal)]
        public Vector2 BlendRange;

        public readonly bool Equals(TexturedVertex2D other) =>
            Position.Equals(other.Position)
            && TexturePosition.Equals(other.TexturePosition)
            && Colour.Equals(other.Colour)
            && TextureRect.Equals(other.TextureRect)
            && BlendRange.Equals(other.BlendRange);
    }
}
