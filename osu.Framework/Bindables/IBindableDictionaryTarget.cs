// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Framework.Bindables
{
    public interface IBindableDictionaryTarget<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IHasDefaultValue
        where TKey : notnull
    {
        /// <inheritdoc cref="IBindable.GetBoundCopy"/>
        IBindableDictionary<TKey, TValue> GetBoundCopy();
    }
}
