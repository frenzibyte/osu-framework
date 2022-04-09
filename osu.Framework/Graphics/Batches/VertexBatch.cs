﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Batches
{
    public abstract class VertexBatch<T> : IVertexBatch, IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        public List<VertexBuffer<T>> VertexBuffers = new List<VertexBuffer<T>>();

        /// <summary>
        /// The number of vertices in each VertexBuffer.
        /// </summary>
        public int Size { get; }

        private int currentBufferIndex;
        private int rollingVertexIndex;

        private readonly int maxBuffers;

        private VertexBuffer<T> currentVertexBuffer => VertexBuffers[currentBufferIndex];

        protected VertexBatch(int bufferSize, int maxBuffers)
        {
            // Vertex buffers of size 0 don't make any sense. Let's not blindly hope for good behavior of OpenGL.
            Trace.Assert(bufferSize > 0);

            Size = bufferSize;
            this.maxBuffers = maxBuffers;
        }

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (VertexBuffer<T> vbo in VertexBuffers)
                    vbo.Dispose();
            }
        }

        #endregion

        public void ResetCounters()
        {
            currentBufferIndex = 0;
            rollingVertexIndex = 0;
            drawStart = 0;
            drawCount = 0;
        }

        protected abstract VertexBuffer<T> CreateVertexBuffer();

        private int drawStart;
        private int drawCount;

        /// <summary>
        /// Adds a vertex to this <see cref="VertexBatch{T}"/>.
        /// </summary>
        /// <param name="v">The vertex to add.</param>
        public void AddVertex(T v)
        {
            ensureHasBufferSpace();
            currentVertexBuffer.EnqueueVertex(drawStart + drawCount, v);

#if VBO_CONSISTENCY_CHECKS
            Trace.Assert(GetCurrentVertex().Equals(v));
#endif

            Advance();
        }

        internal void Advance()
        {
            ++drawCount;
            ++rollingVertexIndex;
        }

#if VBO_CONSISTENCY_CHECKS
        internal T GetCurrentVertex()
        {
            ensureHasBufferSpace();
            return VertexBuffers[currentBufferIndex].Vertices[drawStart + drawCount].Vertex;
        }
#endif

        public int Draw()
        {
            int count = drawCount;

            while (drawCount > 0)
            {
                int drawEnd = Math.Min(currentVertexBuffer.Size, drawStart + drawCount);
                int currentDrawCount = drawEnd - drawStart;

                currentVertexBuffer.DrawRange(drawStart, drawEnd);
                drawStart += currentDrawCount;
                drawCount -= currentDrawCount;

                if (drawStart == currentVertexBuffer.Size)
                {
                    drawStart = 0;
                    currentBufferIndex++;
                }

                FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
                FrameStatistics.Add(StatisticsCounterType.VerticesDraw, currentDrawCount);
            }

            return count;
        }

        private void ensureHasBufferSpace()
        {
            if (VertexBuffers.Count > currentBufferIndex && drawStart + drawCount >= currentVertexBuffer.Size)
            {
                Draw();
                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
            }

            while (currentBufferIndex >= VertexBuffers.Count)
                VertexBuffers.Add(CreateVertexBuffer());
        }

        public ref VertexGroup<T> BeginGroup(ref VertexGroup<T> vertices, DrawNode node)
        {
            GLWrapper.SetActiveBatch(this);

            ulong frameIndex = GLWrapper.DrawNodeFrameIndices[GLWrapper.ResetIndex];

            bool drawRequired =
                // If this is a new usage.
                vertices.Batch != this
                // Or the DrawNode was newly invalidated.
                || vertices.InvalidationID != node.InvalidationID
                // Or another DrawNode was inserted (and drew vertices) before this one.
                || vertices.StartIndex != rollingVertexIndex
                // Or this usage has been skipped for 1 frame. Another DrawNode may have temporarily overwritten the vertices of this one in the batch.
                || frameIndex - vertices.FrameIndex > 1
                // Or if this node has a different backbuffer draw depth (the DrawNode structure changed elsewhere in the scene graph).
                || node.DrawDepth != vertices.DrawDepth;

            // Some DrawNodes (e.g. PathDrawNode) can reuse the same usage in multiple passes. Attempt to allow this use case.
            if (vertices.Batch == this && frameIndex > 0 && vertices.FrameIndex == frameIndex)
            {
                // Only allowed as long as the batch's current vertex index is at the end of the usage (no other usage happened in-between).
                if (rollingVertexIndex != vertices.StartIndex + vertices.Count)
                    throw new InvalidOperationException("Todo:");

                return ref vertices;
            }

            if (drawRequired)
            {
                vertices = new VertexGroup<T>(
                    this,
                    node.InvalidationID,
                    rollingVertexIndex,
                    node.DrawDepth);
            }

            vertices.Count = 0;
            vertices.FrameIndex = frameIndex;
            vertices.DrawRequired = drawRequired;

            return ref vertices;
        }
    }
}
