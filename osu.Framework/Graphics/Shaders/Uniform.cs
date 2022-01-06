// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Shaders
{
    public class Uniform<T> : IUniform where T : unmanaged, IEquatable<T>
    {
        public Shader Owner { get; }
        public string Name { get; }
        public int Location { get; }

        public bool HasChanged { get; private set; } = true;

        private T val;

        public T Value
        {
            get => val;
            set
            {
                if (value.Equals(val))
                    return;

                val = value;
                HasChanged = true;

                if (Owner.IsBound)
                    Update();
            }
        }

        public Uniform(Shader owner, string name, int uniformLocation)
        {
            Owner = owner;
            Name = name;
            Location = uniformLocation;
        }

        public void UpdateValue(ref T newValue)
        {
            if (newValue.Equals(val))
                return;

            val = newValue;
            HasChanged = true;

            if (Owner.IsBound)
                Update();
        }

        public void Update()
        {
            if (!HasChanged) return;

            Renderer.UpdateUniform(this);
            HasChanged = false;
        }

        public ref T GetValueByRef() => ref val;
        public T GetValue() => val;
    }
}
