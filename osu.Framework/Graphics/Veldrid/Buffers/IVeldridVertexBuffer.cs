// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal interface IVeldridVertexBuffer
    {
        /// <summary>
        /// The <see cref="VeldridRenderer.ResetId"/> when this <see cref="IVeldridVertexBuffer"/> was last used.
        /// </summary>
        ulong LastUseResetId { get; }

        /// <summary>
        /// Whether this <see cref="IVeldridVertexBuffer"/> is currently in use.
        /// </summary>
        bool InUse { get; }

        /// <summary>
        /// Frees all resources allocated by this <see cref="IVeldridVertexBuffer"/>.
        /// </summary>
        void Free();
    }
}
