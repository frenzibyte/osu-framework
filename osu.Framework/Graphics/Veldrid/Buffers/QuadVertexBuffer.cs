// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Graphics.Veldrid.Vertices;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal static class QuadIndexData
    {
        private static int maxAmountIndices;

        public static int MaxAmountIndices
        {
            get => maxAmountIndices;
            set
            {
                if (value == maxAmountIndices)
                    return;

                maxAmountIndices = value;

                indexBuffer?.Dispose();
                indexBuffer = null;
            }
        }

        private static DeviceBuffer indexBuffer;

        public static DeviceBuffer IndexBuffer => indexBuffer ??= Vd.Factory.CreateBuffer(new BufferDescription((uint)(MaxAmountIndices * sizeof(ushort)), BufferUsage.IndexBuffer));
    }

    public class QuadVertexBuffer<T> : VertexBuffer<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly int amountIndices;

        private const int indices_per_quad = VeldridTextureSingle.VERTICES_PER_QUAD + 2;

        /// <summary>
        /// The maximum number of quads supported by this buffer.
        /// </summary>
        public const int MAX_QUADS = ushort.MaxValue / indices_per_quad;

        internal QuadVertexBuffer(int amountQuads)
            : base(amountQuads * VeldridTextureSingle.VERTICES_PER_QUAD)
        {
            amountIndices = amountQuads * indices_per_quad;
            Debug.Assert(amountIndices <= ushort.MaxValue);
        }

        protected override void Initialise()
        {
            base.Initialise();

            if (amountIndices > QuadIndexData.MaxAmountIndices)
            {
                ushort[] indices = new ushort[amountIndices];

                for (ushort i = 0, j = 0; j < amountIndices; i += VeldridTextureSingle.VERTICES_PER_QUAD, j += indices_per_quad)
                {
                    indices[j] = i;
                    indices[j + 1] = (ushort)(i + 1);
                    indices[j + 2] = (ushort)(i + 3);
                    indices[j + 3] = (ushort)(i + 2);
                    indices[j + 4] = (ushort)(i + 3);
                    indices[j + 5] = (ushort)(i + 1);
                }

                QuadIndexData.MaxAmountIndices = amountIndices;

                Vd.UpdateBuffer(QuadIndexData.IndexBuffer, 0, ref indices[0], indices.Length * sizeof(ushort));
            }
        }

        public override void Bind()
        {
            base.Bind();
            Vd.BindIndexBuffer(QuadIndexData.IndexBuffer, IndexFormat.UInt16);
        }

        protected override int ToElements(int vertices) => 3 * vertices / 2;

        protected override int ToElementIndex(int vertexIndex) => 3 * vertexIndex / 2;

        protected override PrimitiveTopology Topology => PrimitiveTopology.TriangleList;
    }
}
