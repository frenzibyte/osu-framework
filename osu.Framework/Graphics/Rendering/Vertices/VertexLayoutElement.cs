// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    /// <summary>
    /// Represents one element of a vertex structure layout.
    /// </summary>
    public class VertexLayoutElement
    {
        /// <summary>
        /// The name of this vertex element.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The data type of this vertex element.
        /// </summary>
        public Type Type { get; }

        public VertexLayoutElement(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }
}
