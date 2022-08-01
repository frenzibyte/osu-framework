// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Utils;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// An interface which can be bound to other <see cref="IBindable"/>s in order to watch for (and react to) <see cref="ICanBeDisabled.Disabled">Disabled</see> changes.
    /// </summary>
    public interface IBindable : IBindableTarget, IUnbindable, ICanBeDisabled, IHasDescription
    {
        /// <summary>
        /// Binds ourselves to another bindable such that we receive any values and value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindableTarget them);

        /// <summary>
        /// An alias of <see cref="BindTo"/> provided for use in object initializer scenarios.
        /// Passes the provided value as the foreign (more permanent) bindable.
        /// </summary>
        sealed IBindableTarget BindTarget
        {
            set => BindTo(value);
        }

        /// <summary>
        /// Creates a new instance of this <see cref="IBindable"/> for use in <see cref="IBindableTarget{T}.GetBoundCopy"/>.
        /// The returned instance must have match the most derived type of the bindable class this method is implemented on.
        /// </summary>
        protected IBindable CreateInstance();

        /// <summary>
        /// Helper method which implements <see cref="IBindableTarget{T}.GetBoundCopy"/> for use in final classes.
        /// </summary>
        /// <param name="source">The source <see cref="IBindable"/>.</param>
        /// <typeparam name="T">The bindable type.</typeparam>
        /// <returns>The bound copy.</returns>
        protected static T GetBoundCopyImplementation<T>(T source)
            where T : IBindable
        {
            var copy = source.CreateInstance();

            if (copy.GetType() != source.GetType())
            {
                ThrowHelper.ThrowInvalidOperationException($"Attempted to create a copy of {source.GetType().ReadableName()}, but the returned instance type was {copy.GetType().ReadableName()}. "
                                                           + $"Override {source.GetType().ReadableName()}.{nameof(CreateInstance)}() for {nameof(GetBoundCopy)}() to function properly.");
            }

            copy.BindTo(source);
            return (T)copy;
        }
    }

    /// <summary>
    /// An interface which can be bound to other <see cref="IBindable{T}"/>s in order to watch for (and react to) <see cref="ICanBeDisabled.Disabled">Disabled</see> and <see cref="IBindable{T}.Value">Value</see> changes.
    /// </summary>
    /// <typeparam name="T">The type of value encapsulated by this <see cref="IBindable{T}"/>.</typeparam>
    public interface IBindable<T> : IBindableTarget<T>, IUnbindable, ICanBeDisabled, IHasDescription
    {
        /// <summary>
        /// An event which is raised when <see cref="IBindableTarget{T}.Value"/> has changed.
        /// </summary>
        event Action<ValueChangedEvent<T>> ValueChanged;

        /// <summary>
        /// Binds ourselves to another bindable such that we receive any values and value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindableTarget<T> them);

        /// <summary>
        /// An alias of <see cref="BindTo"/> provided for use in object initializer scenarios.
        /// Passes the provided value as the foreign (more permanent) bindable.
        /// </summary>
        IBindableTarget<T> BindTarget
        {
            set => BindTo(value);
        }

        /// <summary>
        /// Bind an action to <see cref="ValueChanged"/> with the option of running the bound action once immediately.
        /// </summary>
        /// <param name="onChange">The action to perform when <see cref="IBindableTarget{T}.Value"/> changes.</param>
        /// <param name="runOnceImmediately">Whether the action provided in <paramref name="onChange"/> should be run once immediately.</param>
        void BindValueChanged(Action<ValueChangedEvent<T>> onChange, bool runOnceImmediately = false);
    }
}
