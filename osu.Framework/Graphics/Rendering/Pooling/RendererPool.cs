// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Rendering.Pooling
{
    /// <summary>
    /// A pool managing over device resources, designed to handle GPU-side memory access.
    /// </summary>
    /// <typeparam name="TRequest">The pool request type.</typeparam>
    /// <typeparam name="TResource">The device resource type.</typeparam>
    public abstract class RendererPool<TRequest, TResource> : IRendererPool
        where TRequest : struct
    {
        /// <summary>
        /// The list of available or partially-used <typeparamref name="TResource"/>s for use in pool, in order of usage.
        /// </summary>
        protected readonly LinkedList<(ulong useId, TResource resource)> AvailableResources = new LinkedList<(ulong, TResource)>();

        /// <summary>
        /// The list of fully used <typeparamref name="TResource"/>s, in order of usage.
        /// </summary>
        protected readonly LinkedList<(ulong useId, TResource resource)> UsedResources = new LinkedList<(ulong, TResource)>();

        private readonly GlobalStatistic<int> statAvailableCount;
        private readonly GlobalStatistic<int> statUsedCount;

        public bool HasResources => AvailableResources.Count > 0 || UsedResources.Count > 0;

        /// <summary>
        /// Creates a new <see cref="RendererPool{TRequest, TResource}"/>.
        /// </summary>
        /// <param name="name">The pool name in plural form for <see cref="GlobalStatistics"/> display purposes.</param>
        protected RendererPool(string name)
        {
            statAvailableCount = GlobalStatistics.Get<int>("Renderer Pools", $"Available {name.ToLower()}");
            statUsedCount = GlobalStatistics.Get<int>("Renderer Pools", $"Used {name.ToLower()}");
        }

        /// <summary>
        /// Returns a <typeparamref name="TResource"/> satisfying the specified <typeparamref name="TRequest"/> from the pool.
        /// </summary>
        /// <param name="request">The <typeparamref name="TRequest"/> to return a suitable <typeparamref name="TResource"/> for.</param>
        protected TResource Get(TRequest request)
        {
            var node = AvailableResources.First;

            while (node != null)
            {
                if (CanUseResource(request, node.Value.resource))
                {
                    node.Value = (Renderer.ResetId, node.Value.resource);
                    break;
                }

                node = node.Next;
            }

            node ??= new LinkedListNode<(ulong, TResource)>((Renderer.ResetId, CreateResource(request)));

            if (!CanResourceRemainAvailable(request, node.Value.resource))
            {
                if (node.List != null)
                {
                    AvailableResources.Remove(node);
                    statAvailableCount.Value--;
                }

                UsedResources.AddLast(node);
                statUsedCount.Value++;
            }
            else if (node.List == null)
            {
                AvailableResources.AddLast(node);
                statAvailableCount.Value++;
            }

            return node.Value.resource;
        }

        /// <summary>
        /// Whether an existing <typeparamref name="TResource"/> can be used for the specified <typeparamref name="TRequest"/>.
        /// </summary>
        /// <param name="request">The <typeparamref name="TRequest"/> to reuse the resource for.</param>
        /// <param name="resource">The <typeparamref name="TResource"/> to reuse.</param>
        protected virtual bool CanUseResource(TRequest request, TResource resource) => true;

        /// <summary>
        /// Whether a <typeparamref name="TResource"/> can remain available after fulfilling the specified <typeparamref name="TRequest"/>.
        /// </summary>
        /// <param name="request">The <typeparamref name="TRequest"/> to check with.</param>
        /// <param name="resource">The <typeparamref name="TResource"/> to check against.</param>
        protected virtual bool CanResourceRemainAvailable(TRequest request, TResource resource) => false;

        /// <summary>
        /// Creates a new <typeparamref name="TResource"/> for the specified <typeparamref name="TRequest"/>.
        /// </summary>
        /// <param name="request">The <typeparamref name="TRequest"/> to create the resource for.</param>
        protected abstract TResource CreateResource(TRequest request);

        public virtual void ReleaseUsedResources(ulong untilId)
        {
            if (typeof(IRendererPool).IsAssignableFrom(typeof(TResource)))
            {
                foreach (var available in AvailableResources)
                    ((IRendererPool)available.resource).ReleaseUsedResources(untilId);

                foreach (var used in UsedResources)
                    ((IRendererPool)used.resource).ReleaseUsedResources(untilId);
            }

            var pivot = AvailableResources.First;

            for (var node = UsedResources.First; node?.Value.useId <= untilId; node = UsedResources.First)
            {
                UsedResources.Remove(node);
                statUsedCount.Value--;

                while (pivot?.Value.useId <= node.Value.useId)
                    pivot = pivot.Next;

                if (pivot != null)
                    AvailableResources.AddBefore(pivot, node);
                else
                    AvailableResources.AddLast(node);

                statAvailableCount.Value++;
            }
        }

        public virtual bool FreeUnusedResources(ulong resourceFreeInterval)
        {
            bool freed = false;

            for (var node = AvailableResources.First; Renderer.ResetId - node?.Value.useId > resourceFreeInterval; node = AvailableResources.First)
            {
                if (node.Value.resource is IRendererPool pool)
                {
                    pool.FreeUnusedResources(resourceFreeInterval);

                    if (pool.HasResources)
                        continue;
                }

                if (node.Value.resource is IDisposable disposableResource)
                    disposableResource.Dispose();

                AvailableResources.Remove(node);
                statAvailableCount.Value--;
                freed = true;
            }

            return freed;
        }
    }

    public abstract class RendererPool<TResource> : RendererPool<RendererPool<TResource>.EmptyRequest, TResource>
    {
        protected RendererPool(string name)
            : base(name)
        {
        }

        public TResource Get() => Get(default);

        /// <summary>
        /// Whether an existing <typeparamref name="TResource"/> can be reused.
        /// </summary>
        /// <param name="resource">The <typeparamref name="TResource"/> to reuse.</param>
        protected virtual bool CanUseResource(TResource resource) => base.CanUseResource(default, resource);

        protected sealed override bool CanUseResource(EmptyRequest _, TResource resource) => CanUseResource(resource);

        /// <summary>
        /// Whether a <typeparamref name="TResource"/> can remain available.
        /// </summary>
        /// <param name="resource">The <typeparamref name="TResource"/> to check against.</param>
        protected virtual bool CanResourceRemainAvailable(TResource resource) => base.CanResourceRemainAvailable(default, resource);

        protected sealed override bool CanResourceRemainAvailable(EmptyRequest _, TResource resource) => CanResourceRemainAvailable(resource);

        /// <summary>
        /// Creates a new <typeparamref name="TResource"/>.
        /// </summary>
        protected abstract TResource CreateResource();

        protected sealed override TResource CreateResource(EmptyRequest _) => CreateResource();

        public struct EmptyRequest
        {
        }
    }
}
