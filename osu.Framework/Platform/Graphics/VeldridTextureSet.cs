// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Veldrid;

namespace osu.Framework.Platform.Graphics
{
    /// <summary>
    /// A <see cref="ResourceSet"/> consisting of a list of <see cref="Texture"/>s and a backing <see cref="Sampler"/>.
    /// </summary>
    public class VeldridTextureSet : IDisposable
    {
        private readonly ResourceSet resourceSet;
        private readonly bool disposeSamplerOnDisposal;

        /// <summary>
        /// The list of <see cref="Texture"/>s in this resource set.
        /// </summary>
        public IReadOnlyList<Texture> Textures { get; }

        /// <summary>
        /// The <see cref="Texture"/> of this resource set, if <see cref="Textures"/> consists of a single texture.
        /// </summary>
        public Texture Texture => Textures.Single();

        /// <summary>
        /// The backing <see cref="Sampler"/>.
        /// </summary>
        public Sampler Sampler { get; }

        /// <summary>
        /// The <see cref="ResourceLayout"/> of this resource set.
        /// </summary>
        public ResourceLayout Layout { get; }

        public VeldridTextureSet(ResourceSet set, ResourceLayout layout, Texture texture, Sampler sampler, bool disposeSamplerOnDisposal = true)
            : this(set, layout, new[] { texture }, sampler, disposeSamplerOnDisposal)
        {
        }

        public VeldridTextureSet(ResourceSet set, ResourceLayout layout, IReadOnlyList<Texture> textures, Sampler sampler, bool disposeSamplerOnDisposal = true)
        {
            this.disposeSamplerOnDisposal = disposeSamplerOnDisposal;

            resourceSet = set;

            Textures = textures;
            Sampler = sampler;
            Layout = layout;
        }

        public static implicit operator ResourceSet(VeldridTextureSet textureSet) => textureSet.resourceSet;

        public void Dispose()
        {
            if (disposeSamplerOnDisposal)
                Sampler.Dispose();

            foreach (var texture in Textures)
                texture.Dispose();

            Layout?.Dispose();
            resourceSet?.Dispose();
        }
    }
}
