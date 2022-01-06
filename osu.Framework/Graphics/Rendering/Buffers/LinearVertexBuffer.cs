// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Buffers
{
    internal static class LinearIndexData
    {
        public static int MaxAmountIndices;

        private static DeviceBuffer indexBuffer;

        public static DeviceBuffer IndexBuffer => indexBuffer ??= Renderer.Factory.CreateBuffer(new BufferDescription((uint)(MaxAmountIndices * sizeof(ushort)), BufferUsage.IndexBuffer));
    }

    /// <summary>
    /// This type of vertex buffer lets the ith vertex be referenced by the ith index.
    /// </summary>
    public class LinearVertexBuffer<T> : VertexBuffer<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly int amountVertices;

        internal LinearVertexBuffer(int amountVertices, PrimitiveTopology type)
            : base(amountVertices)
        {
            this.amountVertices = amountVertices;
            Topology = type;
        }

        protected override void Initialise()
        {
            base.Initialise();

            if (amountVertices > LinearIndexData.MaxAmountIndices)
            {
                ushort[] indices = new ushort[amountVertices];

                for (ushort i = 0; i < amountVertices; i++)
                    indices[i] = i;

                LinearIndexData.MaxAmountIndices = amountVertices;

                Renderer.BindIndexBuffer(LinearIndexData.IndexBuffer, IndexFormat.UInt16);

                Renderer.UpdateBuffer(LinearIndexData.IndexBuffer, 0, ref indices[0], indices.Length * sizeof(ushort));
            }
        }

        public override void Bind()
        {
            base.Bind();
            Renderer.BindIndexBuffer(LinearIndexData.IndexBuffer, IndexFormat.UInt16);
        }

        protected override PrimitiveTopology Topology { get; }
    }
}
