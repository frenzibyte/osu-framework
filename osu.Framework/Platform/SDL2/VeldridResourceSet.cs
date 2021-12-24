// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;

namespace osu.Framework.Platform.SDL2
{
    public class VeldridResourceSet
    {
        private readonly ResourceSetDescription description;
        private readonly ResourceLayoutDescription resourceLayoutDescription;

        private ResourceSet resourceSet;

        /// <summary>
        /// The resource layout of the resource set.
        /// </summary>
        public ResourceLayout ResourceLayout { get; }

        public VeldridResourceSet(ResourceLayoutDescription resourceLayoutDescription)
        {
            this.resourceLayoutDescription = resourceLayoutDescription;

            ResourceLayout = Vd.Factory.CreateResourceLayout(resourceLayoutDescription);

            description = new ResourceSetDescription(ResourceLayout, new BindableResource[resourceLayoutDescription.Elements.Length]);
        }

        /// <summary>
        /// Gets the layout of the <see cref="ResourceKind"/> from the <see cref="ResourceLayout"/>.
        /// </summary>
        /// <param name="kind">The resource kind.</param>
        /// <param name="index">The index of the layout element.</param>
        /// <returns>The layout of the kind.</returns>
        public ResourceLayoutElementDescription GetLayout(ResourceKind kind, out int index)
        {
            index = Array.FindIndex(resourceLayoutDescription.Elements, e => e.Kind == kind);
            return resourceLayoutDescription.Elements[index];
        }

        private bool requiresReinstantiation;

        /// <summary>
        /// Sets a <see cref="BindableResource"/> of a specified <see cref="ResourceKind"/> to the resource set.
        /// </summary>
        /// <param name="kind">The resource kind.</param>
        /// <param name="resource">The resource set.</param>
        public void SetResource(ResourceKind kind, BindableResource resource)
        {
            int index = Array.FindIndex(resourceLayoutDescription.Elements, e => e.Kind == kind);

            if (resource == description.BoundResources[index])
                return;

            description.BoundResources[index] = resource;
            requiresReinstantiation = true;
        }

        /// <summary>
        /// Returns an updated <see cref="ResourceSet"/> instance for consumption.
        /// </summary>
        public ResourceSet GetResourceSet()
        {
            if (requiresReinstantiation)
            {
                resourceSet?.Dispose();
                resourceSet = Vd.Factory.CreateResourceSet(description);

                requiresReinstantiation = false;
            }

            return resourceSet;
        }
    }
}
