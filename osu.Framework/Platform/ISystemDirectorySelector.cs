// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;

namespace osu.Framework.Platform
{
    public interface ISystemDirectorySelector : IDisposable
    {
        /// <summary>
        /// Triggered when the user has selected a directory.
        /// </summary>
        event Action<DirectoryInfo> Selected;

        /// <summary>
        /// Triggered when the user has cancelled/dismissed the selector.
        /// </summary>
        event Action Cancelled;

        /// <summary>
        /// Presents the system directory selector.
        /// </summary>
        void Present();
    }
}
