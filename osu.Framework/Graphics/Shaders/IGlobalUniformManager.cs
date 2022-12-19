// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable
using System;

namespace osu.Framework.Graphics.Shaders
{
    public interface IGlobalUniformManager
    {
        /// <summary>
        /// Sets a uniform value accessible by all shaders.
        /// <para>Any future-initialized shaders will also have this uniform set.</para>
        /// </summary>
        /// <param name="property">The uniform.</param>
        /// <param name="value">The uniform value.</param>
        protected internal void Set<T>(GlobalProperty property, T value) where T : unmanaged, IEquatable<T>;
    }
}
