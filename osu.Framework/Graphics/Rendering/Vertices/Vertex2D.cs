﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex2D : IEquatable<Vertex2D>, IVertex
    {
        public Vector2 Position;

        public Color4 Colour;

        public readonly bool Equals(Vertex2D other) => Position.Equals(other.Position) && Colour.Equals(other.Colour);
    }
}
