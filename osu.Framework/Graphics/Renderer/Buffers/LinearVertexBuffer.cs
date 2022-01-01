// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Renderer.Vertices;
using Veldrid;
using Vd = osu.Framework.Graphics.Renderer.VeldridGraphicsBackend;

namespace osu.Framework.Graphics.Renderer.Buffers
{
    internal static class LinearIndexData
    {
        public static int MaxAmountIndices;

        private static DeviceBuffer indexBuffer;

        public static DeviceBuffer IndexBuffer => indexBuffer ??= Vd.Factory.CreateBuffer(new BufferDescription((uint)(MaxAmountIndices * sizeof(ushort)), BufferUsage.IndexBuffer));
    }

    /// <summary>
    /// This type of vertex buffer lets the ith vertex be referenced by the ith index.
    /// </summary>
    public class LinearVertexBuffer<T> : VertexBuffer<T>
        where T : struct, IEquatable<T>, IVertex
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

                Vd.BindIndexBuffer(LinearIndexData.IndexBuffer, IndexFormat.UInt16);

                Vd.Commands.UpdateBuffer(LinearIndexData.IndexBuffer, 0, indices);
            }
        }

        public override void Bind(bool forRendering)
        {
            base.Bind(forRendering);

            if (forRendering)
                Vd.BindIndexBuffer(LinearIndexData.IndexBuffer, IndexFormat.UInt16);
        }

        protected override PrimitiveTopology Topology { get; }
    }
}
