// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Textures
{
    /// <summary>
    /// A <see cref="ResourceSet"/> consisting of a <see cref="Texture"/> and a backing <see cref="Sampler"/>.
    /// </summary>
    public class TextureResourceSet : IDisposable
    {
        public IReadOnlyList<Texture> Textures { get; }
        public Texture Texture => Textures.Single();

        public Sampler Sampler { get; }

        public ResourceLayout Layout { get; }

        private readonly ResourceSet resourceSet;

        public TextureResourceSet(Texture texture, Sampler sampler)
            : this(new[] { texture }, sampler)
        {
        }

        public TextureResourceSet(Texture[] textures, Sampler sampler)
        {
            Textures = textures;
            Sampler = sampler;

            Layout = Renderer.GetTextureResourceLayout(textures.Length);

            resourceSet = Renderer.Factory.CreateResourceSet(new ResourceSetDescription(Layout, textures.Append<BindableResource>(sampler).ToArray()));
        }

        public void Dispose()
        {
            resourceSet?.Dispose();
        }

        public static implicit operator ResourceSet(TextureResourceSet textureSet) => textureSet.resourceSet;
    }
}
