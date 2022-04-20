// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Vertices;
using Veldrid;

namespace osu.Framework.Graphics.Batches
{
    public class LinearBatch<T> : VertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly PrimitiveTopology topology;

        [Obsolete("Use `LinearBatch(int size, PrimitiveToplogy type)` instead.")] // Can be removed 2022-11-09
        // ReSharper disable once UnusedParameter.Local
        public LinearBatch(int size, int maxBuffers, PrimitiveTopology topology)
            : this(size, topology)
        {
        }

        public LinearBatch(int size, PrimitiveTopology topology)
            : base(size)
        {
            this.topology = topology;
        }

        protected override VertexBuffer<T> CreateVertexBuffer() => new LinearVertexBuffer<T>(Size, topology);
    }
}
