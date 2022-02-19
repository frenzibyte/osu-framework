// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Vertices;

namespace osu.Framework.Graphics.Batches
{
    public class QuadBatch<T> : VertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        public QuadBatch(int size, int maxBuffers)
            : base(size, maxBuffers)
        {
            if (size > QuadVertexBuffer<T>.MAX_QUADS)
                throw new OverflowException($"Attempted to initialise a {nameof(QuadVertexBuffer<T>)} with more than {nameof(QuadVertexBuffer<T>)}.{nameof(QuadVertexBuffer<T>.MAX_QUADS)} quads ({QuadVertexBuffer<T>.MAX_QUADS}).");
        }

        protected override VertexBuffer<T> CreateVertexBuffer() => new QuadVertexBuffer<T>(Size);
    }
}
