// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Statistics;
using Vd = osu.Framework.Graphics.Renderer.VeldridGraphicsBackend;

namespace osu.Framework.Graphics.Renderer.Pooling
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
        /// The list of available or partially-used <typeparamref name="TResource"/>s for use in pool.
        /// </summary>
        protected readonly List<(ulong useId, TResource resource)> AvailableResources = new List<(ulong, TResource)>();

        /// <summary>
        /// The list of fully used <typeparamref name="TResource"/>s.
        /// </summary>
        protected readonly List<(ulong useId, TResource resource)> UsedResources = new List<(ulong, TResource)>();

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
            TResource resource = default;
            ulong useId = 0;
            int index = -1;

            for (int i = 0; i < AvailableResources.Count; i++)
            {
                var available = AvailableResources[i];

                if (CanUseResource(request, available.resource))
                {
                    useId = available.useId;
                    resource = available.resource;
                    index = i;
                    break;
                }
            }

            if (index == -1)
                resource = CreateResource(request);

            if (IsResourceStillAvailable(resource))
            {
                if (index >= 0)
                    AvailableResources[index] = (Vd.ResetId, resource);
                else
                {
                    AvailableResources.Add((Vd.ResetId, resource));
                    statAvailableCount.Value++;
                }
            }
            else
            {
                if (AvailableResources.Remove((useId, resource)))
                    statAvailableCount.Value--;

                UsedResources.Add((Vd.ResetId, resource));
                statUsedCount.Value++;
            }

            return resource;
        }

        /// <summary>
        /// Whether an existing <typeparamref name="TResource"/> can be used for the specified <typeparamref name="TRequest"/>.
        /// </summary>
        /// <param name="request">The <typeparamref name="TRequest"/> to reuse the resource for.</param>
        /// <param name="resource">The <typeparamref name="TResource"/> to reuse.</param>
        protected virtual bool CanUseResource(TRequest request, TResource resource) => true;

        /// <summary>
        /// Whether a <typeparamref name="TResource"/> can still be used for subsequent requests.
        /// </summary>
        /// <param name="resource">The <typeparamref name="TResource"/> to check against.</param>
        protected virtual bool IsResourceStillAvailable(TResource resource) => false;

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

            int released = UsedResources.RemoveAll(used =>
            {
                if (used.useId <= untilId)
                {
                    AvailableResources.Add(used);
                    return true;
                }

                return false;
            });

            statUsedCount.Value -= released;
            statAvailableCount.Value += released;
        }

        public virtual bool FreeUnusedResources(ulong resourceFreeInterval)
        {
            int freed = AvailableResources.RemoveAll(available =>
            {
                if (Vd.ResetId - available.useId <= resourceFreeInterval)
                    return false;

                if (available.resource is IRendererPool pool)
                {
                    pool.FreeUnusedResources(resourceFreeInterval);

                    if (pool.HasResources)
                        return false;
                }

                if (available.resource is IDisposable disposableResource)
                    disposableResource.Dispose();

                return true;
            });

            statAvailableCount.Value -= freed;
            return freed > 0;
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
        /// Creates a new <typeparamref name="TResource"/>.
        /// </summary>
        protected abstract TResource CreateResource();

        protected sealed override TResource CreateResource(EmptyRequest _) => CreateResource();

        public struct EmptyRequest
        {
        }
    }
}
