// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    [AttributeUsage(AttributeTargets.Field)]
    public class VertexLayoutElement : Attribute
    {
        /// <summary>
        /// The data type of this vertex element.
        /// </summary>
        public VertexElementFormat Format { get; }

        /// <summary>
        /// The semantic of this vertex element.
        /// </summary>
        public VertexElementSemantic Semantic { get; }

        /// <summary>
        /// The name of this vertex element.
        /// </summary>
        internal string Name;

        /// <summary>
        /// The offset of this attribute member in the struct. This is computed internally by the framework.
        /// </summary>
        internal IntPtr Offset;

        public VertexLayoutElement(VertexElementFormat format, VertexElementSemantic semantic)
        {
            Format = format;
            Semantic = semantic;
        }
    }
}
