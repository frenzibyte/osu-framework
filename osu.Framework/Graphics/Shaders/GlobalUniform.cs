// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Shaders
{
    internal class GlobalUniform<T> : IUniform
        where T : unmanaged, IEquatable<T>
    {
        public Shader Owner { get; }
        public int Location { get; }
        public string Name { get; }

        /// <summary>
        /// Non-null denotes a pending global change. Must be a field to allow for reference access.
        /// </summary>
        public UniformMapping<T> PendingChange;

        public GlobalUniform(Shader owner, string name, int uniformLocation)
        {
            Owner = owner;
            Name = name;
            Location = uniformLocation;
        }

        internal void UpdateValue(UniformMapping<T> global)
        {
            PendingChange = global;
            if (Owner.IsBound)
                Update();
        }

        public void Update()
        {
            if (PendingChange == null)
                return;

            Renderer.UpdateUniform(this);
            PendingChange = null;
        }

        public ref T GetValueByRef() => ref PendingChange.GetValueByRef();
        public T GetValue() => PendingChange.Value;
    }
}
