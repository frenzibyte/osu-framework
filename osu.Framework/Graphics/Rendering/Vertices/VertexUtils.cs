// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable StaticMemberInGenericType

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    /// <summary>
    /// Helper methods for retrieving the layout and stride of a vertex structure.
    /// </summary>
    public static class VertexUtils<T>
        where T : struct, IVertex
    {
        private static readonly List<VertexLayoutElement> layout = new List<VertexLayoutElement>();

        /// <summary>
        /// The layout of the vertex of type <typeparamref name="T"/>.
        /// </summary>
        public static IReadOnlyList<VertexLayoutElement> Layout => layout;

        /// <summary>
        /// The stride of the vertex of type <typeparamref name="T"/>.
        /// </summary>
        public static readonly int STRIDE = Marshal.SizeOf<T>();

        static VertexUtils()
        {
            getVertexElementsFromAttributes(typeof(T));
        }

        private static void getVertexElementsFromAttributes(Type type)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // int fieldOffset = currentOffset + Marshal.OffsetOf(type, field.Name).ToInt32();

                if (typeof(IVertex).IsAssignableFrom(field.FieldType))
                {
                    // Vertices may contain others, but the attributes of contained vertices belong to the parent when marshalled, so they are recursively added for their parent
                    // Their field offsets must be adjusted to reflect the position of the child attribute in the parent vertex
                    getVertexElementsFromAttributes(field.FieldType);
                }
                else
                    layout.Add(new VertexLayoutElement($"m_{field.Name}", field.FieldType));
            }
        }
    }
}
