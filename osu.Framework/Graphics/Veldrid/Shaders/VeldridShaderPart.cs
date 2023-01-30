// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal class VeldridShaderPart : IShaderPart
    {
        private const string backbuffer_draw_depth_name = "m_BackbufferDrawDepth";

        private readonly string data;

        public ShaderPartType Type { get; }

        private static readonly Regex include_regex = new Regex("^\\s*#\\s*include\\s+[\"<](.*)[\">]");
        private static readonly Regex shader_attribute_location_regex = new Regex(@"(layout\(location\s=\s)(-?\d+)(\)\s(?>attribute|in).*)", RegexOptions.Multiline);

        public VeldridShaderPart(ShaderManager shaders, byte[]? rawData, ShaderPartType type)
        {
            Type = type;
            data = loadShader(rawData, type, shaders);
        }

        public byte[] GetData() => Encoding.UTF8.GetBytes(data);

        /// <summary>
        /// Loads a given shader data and fills the given uniform group as defined by the shader.
        /// </summary>
        /// <param name="rawData">The raw data of the shader.</param>
        /// <param name="type">The shader type.</param>
        /// <param name="shaders">The shader manager for looking up header/internal files.</param>
        /// <param name="mainFile">Whether this is the main file of the shader, used for recursion.</param>
        private static string loadShader(byte[]? rawData, ShaderPartType type, ShaderManager shaders, bool mainFile = true)
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

                        data += loadShader(shaders.LoadRaw(includeName), type, shaders, false) + '\n';
                    }
                    else
                        data += line + '\n';
                }

                if (!mainFile)
                    return data;

                data = loadShader(shaders.LoadRaw("sh_Precision_Internal.h"), type, shaders, false)
                       + "\n"
                       + loadShader(shaders.LoadRaw("sh_GlobalUniforms.h"), type, shaders, false)
                       + data;

                if (type == ShaderPartType.Vertex)
                    data = appendBackbuffer(data, shaders);

                if (!data.StartsWith("#version", StringComparison.Ordinal))
                    data = $"#version 450\n{data}";

                return data;
            }
        }

        /// <summary>
        /// Appends the "sh_Backbuffer_Internal.h" file to the given data.
        /// </summary>
        /// <param name="data">The data to append the header to.</param>
        /// <param name="shaders">The shader manager for looking up the backbuffer file.</param>
        private static string appendBackbuffer(string data, ShaderManager shaders)
        {
            string realMainName = "real_main_" + Guid.NewGuid().ToString("N");

            string backbufferCode = loadShader(shaders.LoadRaw("sh_Backbuffer_Internal.h"), ShaderPartType.Vertex, shaders, false);

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

        public void Dispose()
        {
        }
    }
}
