// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using Veldrid;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Provides an implementation-agnostic interface on the backing graphics API.
    /// </summary>
    public interface IGraphicsBackend
    {
        /// <summary>
        /// The type of the graphics backend.
        /// </summary>
        GraphicsBackend Type { get; }

        /// <summary>
        /// Initialises the graphics backend, given the current window backend.
        /// It is assumed that the window backend has been initialised.
        /// </summary>
        /// <param name="window">The <see cref="IWindow"/> being used for display.</param>
        void Initialise(IWindow window);

        /// <summary>
        /// Retrieves the underlying drawable area of the window.
        /// </summary>
        Size GetDrawableSize();
    }
}
