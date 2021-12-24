// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Shaders
{
    internal struct ShaderUniformInfo : IEquatable<ShaderUniformInfo>
    {
        /// <summary>
        /// The requested uniform name.
        /// </summary>
        public string Name;

        /// <summary>
        /// The requested uniform data type.
        /// </summary>
        public string Type;

        /// <summary>
        /// The requested uniform data precision.
        /// </summary>
        public string Precision;

        public bool Equals(ShaderUniformInfo other) => Name == other.Name;

        public override bool Equals(object obj) => obj is ShaderUniformInfo other && Equals(other);

        public override int GetHashCode() => Name.GetHashCode();
    }
}
