// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics.ES30;
using Veldrid;

namespace osu.Framework.Graphics.Renderer.Vertices
{
    [AttributeUsage(AttributeTargets.Field)]
    public class VertexMemberAttribute : Attribute
    {
        /// <summary>
        /// The type of each component of this vertex attribute member.
        /// E.g. a <see cref="osuTK.Vector2"/> is represented by 2 **<see cref="VertexAttribPointerType.Float"/>** components.
        /// </summary>
        public VertexElementFormat Format { get; }

        /// <summary>
        /// The semantic of this vertex member attribute.
        /// </summary>
        public VertexElementSemantic Semantic { get; }

        /// <summary>
        /// Whether this vertex attribute member is normalized. If this is set to true, the member will be mapped to
        /// a range of [-1, 1] (signed) or [0, 1] (unsigned) when it is passed to the shader.
        /// </summary>
        public bool Normalized { get; }

        /// <summary>
        /// The offset of this attribute member in the struct. This is computed internally by the framework.
        /// </summary>
        internal IntPtr Offset;

        public VertexMemberAttribute(VertexElementFormat format, VertexElementSemantic semantic, bool normalized = false)
        {
            Format = format;
            Semantic = semantic;
            Normalized = normalized;
        }
    }
}
