// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform
{
    /// <summary>
    /// OpenGL-specific options to pass to an <see cref="IGraphicsBackend"/>.
    /// </summary>
    public readonly struct OpenGLOptions
    {
        /// <summary>
        /// Returns the address of a GL function with the specified name.
        /// </summary>
        public Func<string, IntPtr> GetProcAddress { get; }

        /// <summary>
        /// Creates a new GL context.
        /// </summary>
        public Func<IntPtr> CreateContext { get; }

        /// <summary>
        /// Makes the specified GL context current.
        /// </summary>
        public Action<IntPtr> MakeCurrent { get; }

        /// <summary>
        /// Clears the current GL context.
        /// </summary>
        public Action ClearCurrent { get; }

        /// <summary>
        /// Deletes the specified GL context.
        /// </summary>
        public Action<IntPtr> DeleteContext { get; }

        /// <summary>
        /// Returns the current GL context.
        /// </summary>
        public Func<IntPtr> GetCurrentContext { get; }

        /// <summary>
        /// Performs a backbuffer swap.
        /// </summary>
        public Action SwapWindow { get; }

        /// <summary>
        /// Sets the vertical sync state.
        /// </summary>
        public Action<bool> SetVerticalSync { get; }

        public OpenGLOptions(Func<string, IntPtr> getProcAddress, Func<IntPtr> createContext, Action<IntPtr> makeCurrent, Action clearCurrent, Action<IntPtr> deleteContext, Func<IntPtr> getCurrentContext, Action swapWindow, Action<bool> setVerticalSync)
        {
            GetProcAddress = getProcAddress;
            CreateContext = createContext;
            MakeCurrent = makeCurrent;
            ClearCurrent = clearCurrent;
            DeleteContext = deleteContext;
            GetCurrentContext = getCurrentContext;
            SwapWindow = swapWindow;
            SetVerticalSync = setVerticalSync;
        }
    }
}
