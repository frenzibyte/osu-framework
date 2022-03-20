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
        private ulong frameIndex;

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
            frameIndex++;
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
            GLWrapper.SetActiveBatch(this);

            ensureHasBufferSpace();
            currentVertexBuffer.EnqueueVertex(drawStart + drawCount, v);

            ++drawCount;
            ++rollingVertexIndex;
        }

        void IVertexBatch.Advance()
        {
            GLWrapper.SetActiveBatch(this);

            ensureHasBufferSpace();

            ++drawCount;
            ++rollingVertexIndex;
        }

        public int Draw()
        {
            if (drawCount == 0)
                return 0;

            currentVertexBuffer.DrawRange(drawStart, drawStart + drawCount);

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
            FrameStatistics.Add(StatisticsCounterType.VerticesDraw, drawCount);

            int lastDrawCount = drawCount;

            drawStart += drawCount;
            drawCount = 0;

            return lastDrawCount;
        }

        private void ensureHasBufferSpace()
        {
            // Draw the current vertex buffer if no more vertices can be added to it.
            if (VertexBuffers.Count > 0 && drawStart + drawCount >= currentVertexBuffer.Size)
            {
                Draw();

                // Move to a new vertex buffer.
                currentBufferIndex++;
                drawStart = 0;
                drawCount = 0;

                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
            }

            while (currentBufferIndex >= VertexBuffers.Count)
                VertexBuffers.Add(CreateVertexBuffer());
        }

        public ref VertexBatchUsage<T> BeginUsage(ref VertexBatchUsage<T> usage, DrawNode node)
        {
            bool drawRequired =
                // If this is a new usage...
                usage.Batch != this
                // Or the DrawNode was newly invalidated...
                || usage.InvalidationID != node.InvalidationID
                // Or another DrawNode was inserted (and drew vertices) before this one...
                || usage.StartIndex != rollingVertexIndex
                // Or this usage is more than 1 frame behind. For example, another DrawNode may have temporarily overwritten the vertices of this one in the batch.
                || node.DrawIndex - usage.DrawIndex > 1;

            // Some DrawNodes (e.g. PathDrawNode) can reuse the same usage in multiple passes. Attempt to allow this use case.
            if (usage.Batch == this && usage.FrameIndex == frameIndex)
            {
                // Only allowed as long as the batch's current vertex index is at the end of the usage (no other usage happened in-between).
                if (rollingVertexIndex != usage.StartIndex + usage.Count)
                    throw new InvalidOperationException("Todo:");

                return ref usage;
            }

            if (drawRequired)
            {
                usage = new VertexBatchUsage<T>(
                    this,
                    node.InvalidationID,
                    rollingVertexIndex);
            }

            usage.DrawRequired = drawRequired;
            usage.DrawIndex = node.DrawIndex;
            usage.FrameIndex = frameIndex;

            return ref usage;
        }
    }

    public struct VertexBatchUsage<T> : IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        internal readonly VertexBatch<T> Batch;
        internal readonly long InvalidationID;
        internal readonly int StartIndex;

        internal ulong DrawIndex;
        internal ulong FrameIndex;
        internal bool DrawRequired;
        internal int Count;

        public VertexBatchUsage(VertexBatch<T> batch, long invalidationID, int startIndex)
        {
            Batch = batch;
            InvalidationID = invalidationID;
            StartIndex = startIndex;

            DrawIndex = 0;
            FrameIndex = 0;
            DrawRequired = false;
            Count = 0;
        }

        public void Add(T vertex)
        {
            if (DrawRequired)
                Batch.AddVertex(vertex);
            else
                ((IVertexBatch)Batch).Advance();

            Count++;
        }

        public void Dispose()
        {
        }
    }
}
