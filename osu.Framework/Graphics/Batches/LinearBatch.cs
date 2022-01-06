// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Renderer.Buffers;
using osu.Framework.Graphics.Renderer.Vertices;
using Veldrid;

namespace osu.Framework.Graphics.Batches
{
    public class LinearBatch<T> : VertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly PrimitiveTopology topology;

        public LinearBatch(int size, int maxBuffers, PrimitiveTopology topology)
            : base(size, maxBuffers)
        {
            this.topology = topology;
        }

        protected override VertexBuffer<T> CreateVertexBuffer() => new LinearVertexBuffer<T>(Size, topology);
    }
}
