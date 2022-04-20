// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Development;
using osu.Framework.Graphics.Veldrid.Vertices;
using osu.Framework.Statistics;
using Veldrid;
using BufferUsage = Veldrid.BufferUsage;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    public abstract class VertexBuffer<T> : IVertexBuffer, IDisposable
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private static readonly int stride = VertexUtils<DepthWrappingVertex<T>>.STRIDE;

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
        internal readonly DepthWrappingVertex<T>[] Vertices;
#endif

        private DeviceBuffer buffer;

        private static readonly DepthWrappingVertex<T>[] upload_queue = new DepthWrappingVertex<T>[1024];

        // ReSharper disable once StaticMemberInGenericType
        private static readonly GlobalStatistic<int> vertex_memory_statistic = GlobalStatistics.Get<int>("Native", $"{nameof(VertexBuffer<T>)}");

        protected VertexBuffer(int amountVertices)
        {
            Size = amountVertices;

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
            Vertices = new DepthWrappingVertex<T>[amountVertices];
#endif
        }

        public void SetVertex(int index, T vertex) => VertexUploadQueue<T>.Enqueue(this, index, vertex);

        /// <summary>
        /// Gets the number of vertices in this <see cref="VertexBuffer{T}"/>.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Initialises this <see cref="VertexBuffer{T}"/>. Guaranteed to be run on the draw thread.
        /// </summary>
        protected virtual void Initialise()
        {
            ThreadSafety.EnsureDrawThread();

            var description = new BufferDescription((uint)(Size * stride), BufferUsage.VertexBuffer);

            buffer = Vd.Factory.CreateBuffer(description);

            int size = Size * stride;
            // Vd.Commands.UpdateBuffer(buffer, 0, IntPtr.Zero, size);
            vertex_memory_statistic.Value += size;

            Vd.RegisterVertexBufferUse(this);
        }

        ~VertexBuffer()
        {
            Vd.ScheduleDisposal(v => v.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            ((IVertexBuffer)this).Free();

            IsDisposed = true;
        }

        public virtual void Bind()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed vertex buffers.");

            if (buffer == null)
                Initialise();

            Vd.BindVertexBuffer(buffer, VertexUtils<DepthWrappingVertex<T>>.Layout);
        }

        public virtual void Unbind()
        {
        }

        protected virtual int ToElements(int vertices) => vertices;

        protected virtual int ToElementIndex(int vertexIndex) => vertexIndex;

        protected abstract PrimitiveTopology Topology { get; }

        public void Draw()
        {
            DrawRange(0, Size);
        }

        public void DrawRange(int startIndex, int endIndex)
        {
            LastUseResetId = Vd.ResetId;

            VertexUploadQueue<T>.Upload();

            Bind();

            int countVertices = endIndex - startIndex;
            Vd.DrawPrimitives(Topology, ToElementIndex(startIndex), ToElements(countVertices));

            Unbind();
        }

        internal void UpdateRange(int start, int count, ref DepthWrappingVertex<T> value)
        {
            if (buffer == null)
                Initialise();

            Vd.UpdateBuffer(buffer, start * stride, ref value, count * stride);
        }

        public ulong LastUseResetId { get; private set; }

        public bool InUse => LastUseResetId > 0;

        void IVertexBuffer.Free()
        {
            if (buffer != null)
            {
                Unbind();

                buffer.Dispose();
                buffer = null;

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
                Vertices.AsSpan().Clear();
#endif

                vertex_memory_statistic.Value -= Size * stride;
            }

            LastUseResetId = 0;
        }
    }
}
