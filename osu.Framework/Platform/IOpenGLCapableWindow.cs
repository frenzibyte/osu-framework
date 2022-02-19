// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid.OpenGL;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Interface for <see cref="IGraphicsBackend"/>s with the capability to run under OpenGL.
    /// </summary>
    public interface IHasOpenGLCapability
    {
        /// <summary>
        /// Prepares the graphics backend for running OpenGL.
        /// </summary>
        /// <param name="info">A resultant information to perform window-specific OpenGL commands.</param>
        void PrepareOpenGL(out OpenGLPlatformInfo info);
    }
}
