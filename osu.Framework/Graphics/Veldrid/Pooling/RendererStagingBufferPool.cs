// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Pooling
{
    internal sealed class RendererStagingBufferPool : RendererPool<RendererStagingBufferPool.Request, DeviceBuffer>
    {
        public RendererStagingBufferPool()
            : base("Staging Buffers")
        {
        }

        /// <summary>
        /// Returns a staging <see cref="DeviceBuffer"/> with at least the specified size from the pool.
        /// </summary>
        /// <param name="size">The required buffer size.</param>
        public DeviceBuffer Get(int size) => Get(new Request { Size = size });

        protected override bool CanUseResource(Request request, DeviceBuffer resource) => request.Size <= resource.SizeInBytes;

        protected override DeviceBuffer CreateResource(Request request)
        {
            var description = new BufferDescription((uint)request.Size, BufferUsage.Staging);
            return Vd.Factory.CreateBuffer(description);
        }

        internal struct Request
        {
            public int Size { get; set; }
        }
    }
}
