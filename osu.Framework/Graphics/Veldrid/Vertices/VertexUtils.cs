// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable StaticMemberInGenericType

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Vertices
{
    /// <summary>
    /// Helper methods for retrieving the stride and layout of a vertex structure.
    /// </summary>
    public static class VertexUtils<T>
        where T : struct, IVertex
    {
        /// <summary>
        /// The stride of the vertex of type <typeparamref name="T"/>.
        /// </summary>
        public static readonly int STRIDE = Marshal.SizeOf(default(T));

        public static VertexLayoutDescription Layout { get; }

        private static readonly List<VertexElementDescription> elements = new List<VertexElementDescription>();

        static VertexUtils()
        {
            getVertexElementsFromAttributes(typeof(T), 0);

            Layout = new VertexLayoutDescription(elements.ToArray());
        }

        private static void getVertexElementsFromAttributes(Type type, int currentOffset)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                int fieldOffset = currentOffset + Marshal.OffsetOf(type, field.Name).ToInt32();

                if (typeof(IVertex).IsAssignableFrom(field.FieldType))
                {
                    // Vertices may contain others, but the attributes of contained vertices belong to the parent when marshalled, so they are recursively added for their parent
                    // Their field offsets must be adjusted to reflect the position of the child attribute in the parent vertex
                    getVertexElementsFromAttributes(field.FieldType, fieldOffset);
                }
                else if (field.IsDefined(typeof(VertexMemberAttribute), true))
                {
                    var attrib = (VertexMemberAttribute)field.GetCustomAttribute(typeof(VertexMemberAttribute));
                    Debug.Assert(attrib != null);

                    elements.Add(new VertexElementDescription($"m_{field.Name}", default, attrib.Format, (uint)fieldOffset));
                }
            }
        }
    }
}
