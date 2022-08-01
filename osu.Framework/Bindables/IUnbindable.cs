// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Interface for objects that support publicly unbinding events or <see cref="IBindable"/>s.
    /// </summary>
    public interface IUnbindable
    {
        /// <summary>
        /// Unbinds all bound events.
        /// </summary>
        void UnbindEvents();

        /// <summary>
        /// Unbinds all bound <see cref="IBindable"/>s.
        /// </summary>
        void UnbindBindings();

        /// <summary>
        /// Calls <see cref="UnbindEvents"/> and <see cref="UnbindBindings"/>
        /// </summary>
        void UnbindAll();

        /// <summary>
        /// Unbinds ourselves from an <see cref="IBindableTarget"/> such that we stop receiving updates it.
        /// The <see cref="IBindableTarget"/> will also stop receiving any events from us.
        /// </summary>
        /// <param name="them">The bind target.</param>
        void UnbindFrom(IBindableTarget them);
    }

    public interface IUnbindable<T> : IUnbindable
    {
        /// <summary>
        /// Unbinds ourselves from an <see cref="IBindableTarget{T}"/> such that we stop receiving updates it.
        /// The <see cref="IBindableTarget{T}"/> will also stop receiving any events from us.
        /// </summary>
        /// <param name="them">The bind target.</param>
        void UnbindFrom(IBindableTarget<T> them);
    }
}
