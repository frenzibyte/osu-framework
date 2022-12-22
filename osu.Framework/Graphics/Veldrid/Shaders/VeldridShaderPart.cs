// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal class VeldridShaderPart : IShaderPart
    {
        private const string backbuffer_draw_depth_name = "m_BackbufferDrawDepth";

        public readonly string Data;

        private readonly VeldridUniformGroup uniforms = new VeldridUniformGroup();

        public IVeldridUniformGroup Uniforms => uniforms;

        public ShaderPartType Type { get; }

        private static readonly Regex include_regex = new Regex("^\\s*#\\s*include\\s+[\"<](.*)[\">]");

        private static readonly Regex shader_vertex_uniforms_regex = new Regex(@"m_VertexUniforms\n{\n((?:\s*(?:lowp|mediump|highp)?\s\w+\s+\w+;\n)*)", RegexOptions.Multiline);
        private static readonly Regex shader_fragment_uniforms_regex = new Regex(@"m_FragmentUniforms\n{\n((?:\s*(?:lowp|mediump|highp)?\s\w+\s+\w+;\n)*)", RegexOptions.Multiline);
        private static readonly Regex shader_attribute_location_regex = new Regex(@"(layout\(location\s=\s)(-?\d+)(\)\s(?>attribute|in).*)", RegexOptions.Multiline);

        public VeldridShaderPart(ShaderManager shaders, byte[]? rawData, ShaderPartType type)
        {
            Type = type;
            Data = loadShader(rawData, type, shaders, uniforms);
        }

        /// <summary>
        /// Loads a given shader data and fills the given uniform group as defined by the shader.
        /// </summary>
        /// <param name="rawData">The raw data of the shader.</param>
        /// <param name="type">The shader type.</param>
        /// <param name="shaders">The shader manager for looking up header/internal files.</param>
        /// <param name="uniforms">The uniform group to fill with the data.</param>
        /// <param name="mainFile">Whether this is the main file of the shader, used for recursion.</param>
        private static string loadShader(byte[]? rawData, ShaderPartType type, ShaderManager shaders, VeldridUniformGroup uniforms, bool mainFile = true)
        {
            if (rawData == null)
                return string.Empty;

            using (MemoryStream ms = new MemoryStream(rawData))
            using (StreamReader sr = new StreamReader(ms))
            {
                string data = string.Empty;

                while (sr.Peek() != -1)
                {
                    string? line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line))
                        continue;

                    Match includeMatch = include_regex.Match(line);

                    if (includeMatch.Success)
                    {
                        string includeName = includeMatch.Groups[1].Value.Trim();

                        data += loadShader(shaders.LoadRaw(includeName), type, shaders, uniforms, false) + '\n';
                    }
                    else
                        data += line + '\n';
                }

                if (!mainFile)
                    return data;

                parseUniforms(data, type, uniforms);

                data = loadShader(shaders.LoadRaw("sh_Precision_Internal.h"), type, shaders, uniforms, false) + "\n" + data;
                data = loadShader(shaders.LoadRaw("sh_GlobalUniforms.h"), type, shaders, uniforms, false) + "\n" + data;

                if (type == ShaderPartType.Vertex)
                    data = appendBackbuffer(data, shaders, uniforms);

                if (type == ShaderPartType.Vertex)
                    data = $"#define OSU_VERTEX_SHADER\n\n{data}";

                if (!data.StartsWith("#version", StringComparison.Ordinal))
                    data = $"#version 450\n\n{data}";

                return data;
            }
        }

        /// <summary>
        /// Appends the "sh_Backbuffer_Internal.h" file to the given data.
        /// </summary>
        /// <param name="data">The data to append the header to.</param>
        /// <param name="shaders">The shader manager for looking up the backbuffer file.</param>
        /// <param name="uniforms">The uniform group to fill with the data.</param>
        private static string appendBackbuffer(string data, ShaderManager shaders, VeldridUniformGroup uniforms)
        {
            string realMainName = "real_main_" + Guid.NewGuid().ToString("N");

            string backbufferCode = loadShader(shaders.LoadRaw("sh_Backbuffer_Internal.h"), ShaderPartType.Vertex, shaders, uniforms, false);

            backbufferCode = backbufferCode.Replace("{{ real_main }}", realMainName);
            data = Regex.Replace(data, @"void main\((.*)\)", $"void {realMainName}()") + backbufferCode + '\n';

            Match attributeLocationMatch = shader_attribute_location_regex.Match(data);

            int count = 0;

            while (attributeLocationMatch.Success)
            {
                int location = int.Parse(attributeLocationMatch.Groups[2].Value);
                if (location == -1)
                    break;

                count = Math.Max(location + 1, count);
                attributeLocationMatch = attributeLocationMatch.NextMatch();
            }

            // place the backbuffer draw depth member at the bottom of the vertex structure.
            Debug.Assert(attributeLocationMatch.Value.Contains(backbuffer_draw_depth_name));
            data = data.Replace(attributeLocationMatch.Value, shader_attribute_location_regex.Replace(attributeLocationMatch.Value, m => m.Groups[1] + count.ToString() + m.Groups[3]));
            return data;
        }

        /// <summary>
        /// Parses all uniform declarations inside the given data, and fills them up in the given uniform group.
        /// </summary>
        /// <param name="data">The data to parse uniform declarations from.</param>
        /// <param name="type">The type of the shader to read uniforms from.</param>
        /// <param name="uniforms">The uniform group to fill the uniforms in.</param>
        private static void parseUniforms(string data, ShaderPartType type, VeldridUniformGroup uniforms)
        {
            Match uniformMatch = type == ShaderPartType.Fragment
                ? shader_fragment_uniforms_regex.Match(data)
                : shader_vertex_uniforms_regex.Match(data);

            if (!uniformMatch.Success)
                return;

            string[] uniformLines = uniformMatch.Groups[1].Value.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in uniformLines)
            {
                string[] parts = line.Trim().Replace(";", string.Empty).Split();
                uniforms.AddUniform(parts[^1], parts[^2], parts.Length > 2 ? parts[^3] : null);
            }
        }

        public void Dispose()
        {
        }
    }
}
