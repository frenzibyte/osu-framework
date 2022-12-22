// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Logging;
using osu.Framework.Threading;
using Veldrid;
using Veldrid.SPIRV;
using static osu.Framework.Threading.ScheduledDelegate;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal class VeldridShader : IShader
    {
        private readonly string name;
        private readonly VeldridShaderPart[] parts;
        private readonly VeldridRenderer renderer;

        private Shader[]? shaders;

        private Dictionary<string, IUniform> vertexUniforms = new Dictionary<string, IUniform>();
        private Dictionary<string, IUniform> fragmentUniforms = new Dictionary<string, IUniform>();

        private DeviceBuffer? vertexUniformBuffer;
        private DeviceBuffer? fragmentUniformBuffer;

        private ResourceSet? uniformResourceSet;

        private readonly ScheduledDelegate shaderInitialiseDelegate;

        public bool IsLoaded => shaders != null;

        public bool IsBound { get; private set; }

        /// <summary>
        /// The underlying Veldrid shaders.
        /// </summary>
        public Shader[] Shaders
        {
            get
            {
                if (!IsLoaded)
                    throw new InvalidOperationException("Can not obtain shader parts for an uninitialised shader.");

                Debug.Assert(shaders != null);
                return shaders;
            }
        }

        /// <summary>
        /// A resource set wrapping uniform buffer objects for binding.
        /// </summary>
        public ResourceSet UniformResourceSet
        {
            get
            {
                if (!IsLoaded)
                    throw new InvalidOperationException("Can not obtain uniform resource set for an uninitialised shader.");

                Debug.Assert(uniformResourceSet != null);
                return uniformResourceSet;
            }
        }

        public VeldridShader(VeldridRenderer renderer, string name, params VeldridShaderPart[] parts)
        {
            this.name = name;
            this.parts = parts;
            this.renderer = renderer;

            renderer.ScheduleExpensiveOperation(shaderInitialiseDelegate = new ScheduledDelegate(initialise));
        }

        internal void EnsureShaderInitialised()
        {
            if (isDisposed)
                throw new ObjectDisposedException(ToString(), "Can not compile a disposed shader.");

            if (shaderInitialiseDelegate.State == RunState.Waiting)
                shaderInitialiseDelegate.RunTask();
        }

        public void Bind()
        {
            if (IsBound)
                return;

            EnsureShaderInitialised();

            renderer.BindShader(this);

            foreach (var uniform in vertexUniforms)
                uniform.Value.Update();

            foreach (var uniform in fragmentUniforms)
                uniform.Value.Update();

            IsBound = true;
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            renderer.UnbindShader(this);
            IsBound = false;
        }

        public Uniform<T> GetUniform<T>(string uniformName)
            where T : unmanaged, IEquatable<T>
        {
            if (isDisposed)
                throw new ObjectDisposedException("Can not retrieve uniforms from a disposed shader.");

            EnsureShaderInitialised();

            if (vertexUniforms.TryGetValue(uniformName, out var uniform))
                return (Uniform<T>)uniform;

            if (fragmentUniforms.TryGetValue(uniformName, out uniform))
                return (Uniform<T>)uniform;

            throw new InvalidOperationException($"No uniform with the name '{uniformName}' exists in shader \"{name}\".");
        }

        public DeviceBuffer GetUniformBuffer(IUniform uniform)
        {
            if (vertexUniforms.ContainsKey(uniform.Name))
                return vertexUniformBuffer.AsNonNull();

            if (fragmentUniforms.ContainsKey(uniform.Name))
                return fragmentUniformBuffer.AsNonNull();

            throw new InvalidOperationException($"No uniform with the name '{uniform.Name}' exists in shader \"{name}\".");
        }

        private void initialise()
        {
            Debug.Assert(parts.Length == 2);

            VeldridShaderPart vertex = parts.Single(p => p.Type == ShaderPartType.Vertex);
            VeldridShaderPart fragment = parts.Single(p => p.Type == ShaderPartType.Fragment);

            vertexUniformBuffer = vertex.Uniforms.CreateBuffer(renderer, this, out vertexUniforms);
            fragmentUniformBuffer = fragment.Uniforms.CreateBuffer(renderer, this, out fragmentUniforms);
            uniformResourceSet = renderer.CreateUniformResourceSet(vertexUniformBuffer, fragmentUniformBuffer);

            var vertexDescription = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertex.Data), "main");
            var fragmentDescription = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragment.Data), "main");

            try
            {
                shaders = renderer.Factory.CreateFromSpirv(vertexDescription, fragmentDescription, new CrossCompileOptions
                {
                    FixClipSpaceZ = !renderer.Device.IsDepthRangeZeroToOne,
                    InvertVertexOutputY = renderer.Device.IsClipSpaceYInverted,
                });
            }
            catch (SpirvCompilationException e)
            {
                Logger.Error(e, $"Failed to initialise shader \"{name}\"");
            }
        }

        private bool isDisposed;

        ~VeldridShader()
        {
            renderer.ScheduleDisposal(s => s.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            if (shaders != null)
            {
                for (int i = 0; i < shaders.Length; i++)
                    shaders[i].Dispose();

                shaders = null;
            }
        }
    }
}
