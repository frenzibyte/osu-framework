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
    public struct TexturedVertex3D : IEquatable<TexturedVertex3D>, IVertex
    {
        [VertexMember(VertexElementFormat.Float3)]
        public Vector3 Position;

        [VertexMember(VertexElementFormat.Float4)]
        public Color4 Colour;

        [VertexMember(VertexElementFormat.Float2)]
        public Vector2 TexturePosition;

        public readonly bool Equals(TexturedVertex3D other) => Position.Equals(other.Position) && TexturePosition.Equals(other.TexturePosition) && Colour.Equals(other.Colour);
    }
}
