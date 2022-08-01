// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// An <see cref="IBindable"/> exposed as a bind target for another <see cref="IBindable{T}"/>.
    /// </summary>
    public interface IBindableTarget : IHasDefaultValue
    {
        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to instantiate a copy bindable that's not matching the original's type.</exception>
        IBindable GetBoundCopy();
    }

    /// <summary>
    /// An <see cref="IBindable{T}"/> exposed as a bind target for another <see cref="IBindable{T}"/>.
    /// </summary>
    public interface IBindableTarget<T> : IHasDefaultValue
    {
        /// <summary>
        /// The current value of this bindable.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// The default value of this bindable. Used when querying <see cref="IHasDefaultValue.IsDefault"/>.
        /// </summary>
        T Default { get; }

        /// <inheritdoc cref="IBindable.GetBoundCopy"/>
        IBindable<T> GetBoundCopy();
    }
}
