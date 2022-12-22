// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.Shaders;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal interface IVeldridUniformGroup
    {
        /// <summary>
        /// All uniforms declared by any shader part in the <see cref="VeldridShader"/>.
        /// </summary>
        IReadOnlyList<VeldridUniformInfo> Uniforms { get; }

        /// <summary>
        /// Creates a uniform buffer object for all uniforms in this group.
        /// </summary>
        /// <param name="renderer">The renderer to create the uniform buffer.</param>
        /// <param name="owner">The owner of the uniforms, or null for global uniforms.</param>
        /// <param name="uniforms">A list of <see cref="IUniform"/>s instantiated for the buffer structure.</param>
        DeviceBuffer CreateBuffer(VeldridRenderer renderer, VeldridShader? owner, out Dictionary<string, IUniform> uniforms);
    }
}
