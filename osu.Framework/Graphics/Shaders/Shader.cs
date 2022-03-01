﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Threading;
using osuTK;
using Veldrid;
using Veldrid.SPIRV;
using VdShader = Veldrid.Shader;
using static osu.Framework.Threading.ScheduledDelegate;
using Encoding = System.Text.Encoding;

namespace osu.Framework.Graphics.Shaders
{
    public class Shader : IShader, IDisposable
    {
        public readonly string Name;

        private readonly List<ShaderPart> parts;

        private readonly ScheduledDelegate shaderCompileDelegate;

        internal readonly Dictionary<string, IUniform> Uniforms = new Dictionary<string, IUniform>();

        private IReadOnlyList<ShaderUniformInfo> uniformInfo;

        /// <summary>
        /// Holds all the <see cref="Uniforms"/> values for faster access than iterating on <see cref="Dictionary{TKey,TValue}.Values"/>.
        /// </summary>
        private IUniform[] uniformsValues;

        public bool IsLoaded { get; private set; }

        internal bool IsBound { get; private set; }

        internal VdShader[] Shaders { get; private set; }

        internal DeviceBuffer UniformBuffer { get; private set; }

        internal ResourceSet UniformResourceSet { get; private set; }

        internal VertexLayoutDescription VertexLayout { get; private set; }

        internal Shader(string name, List<ShaderPart> parts)
        {
            this.parts = parts;

            Name = name;

            Vd.ScheduleExpensiveOperation(shaderCompileDelegate = new ScheduledDelegate(compile));
        }

        private void compile()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not compile a disposed shader.");

            if (IsLoaded)
                throw new InvalidOperationException("Attempting to compile an already-compiled shader.");

            SetupUniforms();

            CompileInternal();

            IsLoaded = true;

            GlobalPropertyManager.Register(this);
        }

        internal void EnsureShaderCompiled()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not compile a disposed shader.");

            if (shaderCompileDelegate.State == RunState.Waiting)
                shaderCompileDelegate.RunTask();
        }

        public void Bind()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed shader.");

            if (IsBound)
                return;

            EnsureShaderCompiled();

            Vd.BindShader(this);

            foreach (var uniform in uniformsValues)
                uniform?.Update();

            IsBound = true;
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            Vd.UnbindShader(this);

            IsBound = false;
        }

        /// <summary>
        /// Returns a uniform from the shader.
        /// </summary>
        /// <param name="name">The name of the uniform.</param>
        /// <returns>Returns a base uniform.</returns>
        public Uniform<T> GetUniform<T>(string name)
            where T : unmanaged, IEquatable<T>
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not retrieve uniforms from a disposed shader.");

            EnsureShaderCompiled();

            return (Uniform<T>)Uniforms[name];
        }

        private protected virtual void CompileInternal()
        {
            var descriptions = new List<ShaderDescription>();

            foreach (var part in parts)
                descriptions.Add(new ShaderDescription(part.Type, part.GetData(uniformInfo), Vd.Device.BackendType == GraphicsBackend.Metal ? "main0" : "main", false));

            var vertex = descriptions.Single(s => s.Stage == ShaderStages.Vertex);
            var fragment = descriptions.Single(s => s.Stage == ShaderStages.Fragment);

            try
            {
                if (Vd.Device.BackendType == GraphicsBackend.Vulkan)
                {
                    vertex.ShaderBytes = ensureSpirv(vertex);
                    fragment.ShaderBytes = ensureSpirv(fragment);
                }
                else
                {
                    var result = SpirvCompilation.CompileVertexFragment(vertex.ShaderBytes, fragment.ShaderBytes, getCompilationTarget(Vd.Device.BackendType), new CrossCompileOptions(!Vd.Device.IsDepthRangeZeroToOne, false));

                    switch (Vd.Device.BackendType)
                    {
                        case GraphicsBackend.Direct3D11:
                        case GraphicsBackend.OpenGL:
                        case GraphicsBackend.OpenGLES:
                            vertex.ShaderBytes = Encoding.ASCII.GetBytes(result.VertexShader);
                            fragment.ShaderBytes = Encoding.ASCII.GetBytes(result.FragmentShader);
                            break;

                        case GraphicsBackend.Metal:
                            vertex.EntryPoint = "main0";
                            vertex.ShaderBytes = Encoding.UTF8.GetBytes(result.VertexShader);
                            fragment.EntryPoint = "main0";
                            fragment.ShaderBytes = Encoding.UTF8.GetBytes(result.FragmentShader);
                            break;
                    }

                    VertexLayout = new VertexLayoutDescription(result.Reflection.VertexElements);
                }

                Shaders = new[] { Vd.Factory.CreateShader(vertex), Vd.Factory.CreateShader(fragment) };
            }
            catch (SpirvCompilationException sce)
            {
                throw new ShaderCompilationFailedException(Name, sce.Message);
            }
        }

        private protected virtual void SetupUniforms()
        {
            uniformInfo = parts.SelectMany(p => p.Uniforms).Distinct().ToArray();
            uniformsValues = new IUniform[uniformInfo.Count];

            int bufferSize = 0;

            for (int i = 0; i < uniformInfo.Count; i++)
            {
                IUniform uniform;

                switch (uniformInfo[i].Type)
                {
                    case "bool":
                        uniform = createUniform<bool>(this, uniformInfo[i].Name, ref bufferSize);
                        break;

                    case "float":
                        uniform = createUniform<float>(this, uniformInfo[i].Name, ref bufferSize);
                        break;

                    case "int":
                        uniform = createUniform<int>(this, uniformInfo[i].Name, ref bufferSize);
                        break;

                    case "mat3":
                        uniform = createUniform<Matrix3>(this, uniformInfo[i].Name, ref bufferSize);
                        break;

                    case "mat4":
                        uniform = createUniform<Matrix4>(this, uniformInfo[i].Name, ref bufferSize);
                        break;

                    case "vec2":
                        uniform = createUniform<Vector2>(this, uniformInfo[i].Name, ref bufferSize);
                        break;

                    case "vec3":
                        uniform = createUniform<Vector3>(this, uniformInfo[i].Name, ref bufferSize);
                        break;

                    case "vec4":
                        uniform = createUniform<Vector4>(this, uniformInfo[i].Name, ref bufferSize);
                        break;

                    case "texture2D":
                        uniform = createUniform<int>(this, uniformInfo[i].Name, ref bufferSize);
                        break;

                    default:
                        continue;
                }

                Uniforms.Add(uniformInfo[i].Name, uniform);
                uniformsValues[i] = uniform;
            }

            if (bufferSize % 16 > 0)
                bufferSize += 16 - (bufferSize % 16);

            UniformBuffer = Vd.Factory.CreateBuffer(new BufferDescription((uint)bufferSize, BufferUsage.UniformBuffer));
            UniformResourceSet = Vd.CreateUniformResourceSet(UniformBuffer);
        }

        private static IUniform createUniform<T>(Shader shader, string name, ref int bufferSize)
            where T : unmanaged, IEquatable<T>
        {
            int uniformSize = Marshal.SizeOf<T>();
            int baseAlignment;

            // see https://www.khronos.org/registry/OpenGL/specs/gl/glspec45.core.pdf#page=159.
            if (typeof(T) == typeof(Vector3))
            {
                // vec3 has a base alignment of 4N (vec4).
                baseAlignment = uniformSize = Marshal.SizeOf<Vector4>();
            }
            else if (typeof(T) == typeof(Matrix3) || typeof(T) == typeof(Matrix4))
            {
                // mat3 has a base alignment of vec4 for each column.
                if (typeof(T) == typeof(Matrix3))
                    uniformSize = Marshal.SizeOf<Matrix3x4>();

                baseAlignment = Marshal.SizeOf<Vector4>();
            }
            else
                baseAlignment = uniformSize;

            // offset the location of this uniform with the calculated base alignment.
            if (bufferSize % baseAlignment > 0)
                bufferSize += baseAlignment - (bufferSize % baseAlignment);

            int location = bufferSize;

            bufferSize += uniformSize;

            if (GlobalPropertyManager.CheckGlobalExists(name))
                return new GlobalUniform<T>(shader, name, location);

            return new Uniform<T>(shader, name, location);
        }

        public override string ToString() => $@"{Name} Shader (Compiled: {Shaders != null})";

        #region IDisposable Support

        protected internal bool IsDisposed { get; private set; }

        ~Shader()
        {
            Vd.ScheduleDisposal(s => s.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;

                shaderCompileDelegate?.Cancel();

                GlobalPropertyManager.Unregister(this);

                UniformResourceSet?.Dispose();
                UniformResourceSet = null;

                UniformBuffer?.Dispose();
                UniformBuffer = null;

                for (int i = 0; i < Shaders.Length; i++)
                {
                    Shaders[i]?.Dispose();
                    Shaders[i] = null;
                }

                Shaders = null;
            }
        }

        #endregion

        private static CrossCompileTarget getCompilationTarget(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Direct3D11:
                    return CrossCompileTarget.HLSL;

                case GraphicsBackend.OpenGL:
                    return CrossCompileTarget.GLSL;

                case GraphicsBackend.Metal:
                    return CrossCompileTarget.MSL;

                case GraphicsBackend.OpenGLES:
                    return CrossCompileTarget.ESSL;

                default:
                    throw new ArgumentOutOfRangeException(nameof(backend), backend, null);
            }
        }

        private static byte[] ensureSpirv(ShaderDescription description)
        {
            if (description.ShaderBytes[0] == 3 && description.ShaderBytes[1] == 2 && description.ShaderBytes[2] == 35 && description.ShaderBytes[3] == 7)
                return description.ShaderBytes;

            return SpirvCompilation.CompileGlslToSpirv(Encoding.UTF8.GetString(description.ShaderBytes), null, description.Stage, new GlslCompileOptions(description.Debug)).SpirvBytes;
        }

        public class ShaderCompilationFailedException : Exception
        {
            public ShaderCompilationFailedException(string name, string log)
                : base($"{nameof(Shader)} '{name}' failed to compile:\n{log.Trim()}")
            {
            }
        }
    }
}
