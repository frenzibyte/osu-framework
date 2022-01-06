// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// The type of primitive topology to use when drawing.
    /// </summary>
    /// <remarks>
    /// See https://www.khronos.org/opengl/wiki/Primitive for more information.
    /// </remarks>
    public enum PrimitiveTopology
    {
        /// <summary>
        /// Draws one triangle for each group of 3 vertices.
        /// </summary>
        /// <example>
        /// vertices:    0, 1, 2, 3, 4, 5
        /// triangle 1: (0, 1, 2)
        /// triangle 2:          (3, 4, 5)
        /// </example>
        Triangles,

        /// <summary>
        /// Draws one triangle for every possible group of 3 adjacent vertices.
        /// </summary>
        /// <example>
        /// vertices:    0, 1, 2, 3, 4, 5
        /// triangle 1: (0, 1, 2)
        /// triangle 2:    (1, 2, 3)
        /// triangle 3:       (2, 3, 4)
        /// triangle 4:          (3, 4, 5)
        /// </example>
        TriangleStrip,

        /// <summary>
        /// Draws one line for each pair of vertices.
        /// </summary>
        /// <example>
        /// vertices: 0, 1, 2, 3
        /// line 1:  (0, 1)
        /// line 2:        (2, 3)
        /// </example>
        Lines,

        /// <summary>
        /// Draws one line for every possible pair of vertices.
        /// </summary>
        /// <example>
        /// vertices: 0, 1, 2, 3
        /// line 1:  (0, 1)
        /// line 2:     (1, 2)
        /// line 3:        (2, 3)
        /// </example>
        LineStrip,

        /// <summary>
        /// Draws one point for each vertex.
        /// </summary>
        /// <example>
        /// vertices:  0, 1, 2
        /// point 1:  (0)
        /// point 2:     (1)
        /// point 2:        (2)
        /// </example>
        Points,
    }
}
