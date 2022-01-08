// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Represents a renderer state exposed as a <see cref="Stack{T}"/>.
    /// </summary>
    public class RendererState<T>
    {
        private readonly OnValueChangeDelegate onValueChange;
        private readonly Stack<T> stack = new Stack<T>();

        /// <summary>
        /// The current state value.
        /// </summary>
        public T Value => stack.Peek();

        /// <summary>
        /// The number of states pushed.
        /// </summary>
        public int Count => stack.Count;

        public RendererState(OnValueChangeDelegate onValueChange)
        {
            this.onValueChange = onValueChange;
        }

        /// <summary>
        /// Pushes a new state and updates the renderer.
        /// </summary>
        public void Push(T value)
        {
            stack.Push(value);
            onValueChange(value, true);
        }

        /// <summary>
        /// Removes the last pushed state and updates the renderer.
        /// </summary>
        public void Pop()
        {
            Trace.Assert(stack.Count > 1);

            stack.Pop();
            onValueChange(stack.Peek(), false);
        }

        /// <summary>
        /// Clears the renderer states.
        /// </summary>
        public void Clear() => stack.Clear();

        public delegate void OnValueChangeDelegate(T state, bool isPushing);
    }
}
