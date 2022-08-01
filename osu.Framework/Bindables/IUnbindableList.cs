// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Bindables
{
    public interface IUnbindableList<T> : IUnbindable
    {
        /// <summary>
        /// Unbinds ourselves from an <see cref="IBindableListTarget{T}"/> such that we stop receiving updates it.
        /// The <see cref="IBindableListTarget{T}"/> will also stop receiving any events from us.
        /// </summary>
        /// <param name="them">The bind target.</param>
        void UnbindFrom(IBindableListTarget<T> them);
    }
}
