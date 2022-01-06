// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Shaders
{
    /// <summary>
    /// Represents an updateable shader uniform.
    /// </summary>
    public interface IUniform
    {
        /// <summary>
        /// The name of this <see cref="IUniform"/>
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The location of this <see cref="IUniform"/> in the uniform buffer.
        /// </summary>
        int Location { get; }

        /// <summary>
        /// The owner of this <see cref="IUniform"/>.
        /// </summary>
        Shader Owner { get; }

        /// <summary>
        /// Synchronises this <see cref="IUniform"/> with the GPU.
        /// </summary>
        void Update();
    }

    /// <summary>
    /// Represents an updateable read-only typed shader uniform.
    /// </summary>
    /// <typeparam name="T">The uniform value type</typeparam>
    public interface IUniform<T> : IUniform
        where T : unmanaged, IEquatable<T>
    {
        /// <summary>
        /// Returns the value of this <see cref="IUniform{T}"/>.
        /// </summary>
        T GetValue();

        /// <summary>
        /// Returns the value of this <see cref="IUniform{T}"/> by reference.
        /// </summary>
        ref T GetValueByRef();
    }
}
