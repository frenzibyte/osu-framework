// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.Graphics
{
    /// <summary>
    /// Metal-specific options to pass to an <see cref="IGraphicsBackend"/>.
    /// </summary>
    public readonly struct MetalOptions
    {
        /// <summary>
        /// Creates a metal-backed view for use in the graphics backend.
        /// </summary>
        public Func<IntPtr> CreateMetalView { get; }

        public MetalOptions(Func<IntPtr> createMetalView)
        {
            CreateMetalView = createMetalView;
        }
    }
}
