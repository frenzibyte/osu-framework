// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DepthWrappingVertex<TVertex> : IVertex, IEquatable<DepthWrappingVertex<TVertex>>
        where TVertex : unmanaged, IVertex, IEquatable<TVertex>
    {
        public float BackbufferDrawDepth;

        public TVertex Vertex;

        public readonly bool Equals(DepthWrappingVertex<TVertex> other)
            => Vertex.Equals(other.Vertex)
               && BackbufferDrawDepth.Equals(other.BackbufferDrawDepth);
    }
}
