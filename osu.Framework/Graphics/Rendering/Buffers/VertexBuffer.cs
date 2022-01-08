﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Statistics;
using SixLabors.ImageSharp.Memory;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Buffers
{
    public abstract class VertexBuffer<T> : IVertexBuffer, IDisposable
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private IDisposable vertexResource;

        public object VertexResource => vertexResource;

        public abstract object IndexResource { get; }

        protected static readonly int STRIDE = VertexUtils<DepthWrappingVertex<T>>.STRIDE;

        private Memory<DepthWrappingVertex<T>> vertexMemory;
        private IMemoryOwner<DepthWrappingVertex<T>> memoryOwner;

        protected VertexBuffer(int amountVertices)
        {
            Size = amountVertices;
        }

        /// <summary>
        /// Sets the vertex at a specific index of this <see cref="VertexBuffer{T}"/>.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>Whether the vertex changed.</returns>
        public bool SetVertex(int vertexIndex, T vertex)
        {
            ref var currentVertex = ref getMemory().Span[vertexIndex];

            bool isNewVertex = !currentVertex.Vertex.Equals(vertex) || currentVertex.BackbufferDrawDepth != Renderer.BackbufferDrawDepth;

            currentVertex.Vertex = vertex;
            currentVertex.BackbufferDrawDepth = Renderer.BackbufferDrawDepth;

            return isNewVertex;
        }

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

            var description = new BufferDescription((uint)(Size * STRIDE), BufferUsage.VertexBuffer);

            // buffer = Renderer.Factory.CreateBuffer(description);
            //
            // Renderer.BindVertexBuffer(buffer, VertexUtils<DepthWrappingVertex<T>>.Layout);
        }

        ~VertexBuffer()
        {
            Renderer.ScheduleDisposal(v => v.Dispose(false), this);
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

            // Renderer.BindVertexBuffer(buffer, VertexUtils<DepthWrappingVertex<T>>.Layout);
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
            if (buffer == null)
                Initialise();

            Bind();

            int countVertices = endIndex - startIndex;
            Renderer.DrawVertices(Topology, ToElementIndex(startIndex), ToElements(countVertices));

            Unbind();
        }

        public void Update()
        {
            UpdateRange(0, Size);
        }

        public void UpdateRange(int startIndex, int endIndex)
        {
            if (buffer == null)
                Initialise();

            int countVertices = endIndex - startIndex;
            // .UpdateVertexBuffer(this, startIndex * STRIDE, getMemory().Slice(startIndex, countVertices));

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, countVertices);
        }

        private ref Memory<DepthWrappingVertex<T>> getMemory()
        {
            ThreadSafety.EnsureDrawThread();

            if (!InUse)
            {
                memoryOwner = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<DepthWrappingVertex<T>>(Size, AllocationOptions.Clean);
                vertexMemory = memoryOwner.Memory;

                Renderer.RegisterVertexBufferUse(this);
            }

            LastUseResetId = Renderer.ResetId;

            return ref vertexMemory;
        }

        public ulong LastUseResetId { get; private set; }

        public bool InUse => LastUseResetId > 0;

        void IVertexBuffer.Free()
        {
            if (vertexResource != null)
            {
                Unbind();

                vertexResource.Dispose();
                vertexResource = null;
            }

            memoryOwner?.Dispose();
            memoryOwner = null;
            vertexMemory = Memory<DepthWrappingVertex<T>>.Empty;

            LastUseResetId = 0;
        }
    }
}
