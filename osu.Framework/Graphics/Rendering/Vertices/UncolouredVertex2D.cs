// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osuTK;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UncolouredVertex2D : IEquatable<UncolouredVertex2D>, IVertex
    {
        public Vector2 Position;

        public readonly bool Equals(UncolouredVertex2D other) => Position.Equals(other.Position);
    }
}
