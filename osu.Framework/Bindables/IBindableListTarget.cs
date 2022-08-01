// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;

namespace osu.Framework.Bindables
{
    public interface IBindableListTarget<T> : IReadOnlyList<T>, IHasDefaultValue
    {
        /// <inheritdoc cref="IBindable.GetBoundCopy"/>
        IBindableList<T> GetBoundCopy();
    }
}
