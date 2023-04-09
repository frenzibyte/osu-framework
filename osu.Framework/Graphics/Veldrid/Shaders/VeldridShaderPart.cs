// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal class VeldridShaderPart : IShaderPart
    {
        public static readonly Regex SHADER_INPUT_PATTERN =
            new Regex(@"^\s*layout\s*\(\s*location\s*=\s*(-?\d+)\s*\)\s*((?:flat\s*)?in\s+(?:(?:lowp|mediump|highp)\s+)?\w+\s+(\w+)\s*;)", RegexOptions.Multiline);

        private static readonly Regex last_input_pattern = new Regex(@"^\s*layout\s*\(\s*location\s*=\s*-1\s*\)\s+in", RegexOptions.Multiline);
        private static readonly Regex uniform_pattern = new Regex(@"^(\s*layout\s*\(.*)set\s*=\s*(-?\d)(.*\)\s*(?:(?:readonly\s*)?buffer|uniform))", RegexOptions.Multiline);
        private static readonly Regex include_pattern = new Regex(@"^\s*#\s*include\s+[""<](.*)["">]");

        public readonly ShaderPartType Type;

        private readonly List<string> shaderCodes = new List<string>();
        private readonly VeldridRenderer renderer;
        private readonly IShaderStore store;

        public VeldridShaderPart(VeldridRenderer renderer, byte[]? data, ShaderPartType type, IShaderStore store)
        {
            this.renderer = renderer;
            this.store = store;

            Type = type;

            // Load the shader files.
            shaderCodes.Add(loadFile(data, true));

            int lastInputIndex = 0;

            // Parse all shader inputs to find the last input index.
            for (int i = 0; i < shaderCodes.Count; i++)
            {
                foreach (Match m in SHADER_INPUT_PATTERN.Matches(shaderCodes[i]))
                    lastInputIndex = Math.Max(lastInputIndex, int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
            }

            // Update the location of the m_BackbufferDrawDepth input to be placed after all other inputs.
            for (int i = 0; i < shaderCodes.Count; i++)
                shaderCodes[i] = last_input_pattern.Replace(shaderCodes[i], match => $"layout(location = {++lastInputIndex}) in");

            // Find the minimum uniform/buffer binding set across all shader codes. This will be a negative number (see sh_GlobalUniforms.h).
            int minSet = shaderCodes.Select(c =>
            {
                return uniform_pattern.Matches(c).Where(m => m.Success)
                                      .Select(m => int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture))
                                      .DefaultIfEmpty(0).Min();
            }).DefaultIfEmpty(0).Min();

            // Increment the binding set of all uniform blocks such that the minimum index is 0.
            // The difference in implementation here (compared to above) is intentional, as uniform blocks must be consistent between the shader stages, so they can't be easily appended.
            for (int i = 0; i < shaderCodes.Count; i++)
            {
                shaderCodes[i] = uniform_pattern.Replace(shaderCodes[i],
                    match => $"{match.Groups[1].Value}set = {int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) + Math.Abs(minSet)}{match.Groups[3].Value}");
            }
        }

        private string loadFile(byte[]? bytes, bool mainFile)
        {
            if (bytes == null)
                return string.Empty;

            using (MemoryStream ms = new MemoryStream(bytes))
            using (StreamReader sr = new StreamReader(ms))
            {
                string code = string.Empty;

                while (sr.Peek() != -1)
                {
                    string? line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        code += line + '\n';
                        continue;
                    }

                    if (line.StartsWith("#version", StringComparison.Ordinal)) // the version directive has to appear before anything else in the shader
                    {
                        shaderCodes.Insert(0, line + '\n');
                        continue;
                    }

                    if (line.StartsWith("#extension", StringComparison.Ordinal))
                    {
                        shaderCodes.Add(line + '\n');
                        continue;
                    }

                    Match includeMatch = include_pattern.Match(line);

                    if (includeMatch.Success)
                    {
                        string includeName = includeMatch.Groups[1].Value.Trim();

                        //#if DEBUG
                        //                        byte[] rawData = null;
                        //                        if (File.Exists(includeName))
                        //                            rawData = File.ReadAllBytes(includeName);
                        //#endif
                        code += loadFile(store.LoadRaw(includeName), false) + '\n';
                    }
                    else
                        code += line + '\n';
                }

                if (mainFile)
                {
                    string internalIncludes = loadFile(store.LoadRaw("Internal/sh_Compatibility.h"), false) + "\n";

                    internalIncludes += loadFile(store.LoadRaw("Internal/sh_GlobalUniforms.h"), false) + "\n";
                    internalIncludes += loadFile(store.LoadRaw("Internal/sh_MaskingInfo.h"), false) + "\n";

                    if (renderer.Device.Features.StructuredBuffer)
                        internalIncludes += loadFile(store.LoadRaw("Internal/sh_MaskingBuffer_SSBO.h"), false) + "\n";
                    else
                        internalIncludes += loadFile(store.LoadRaw("Internal/sh_MaskingBuffer_UBO.h"), false) + "\n";

                    if (Type == ShaderPartType.Vertex)
                        internalIncludes += loadFile(store.LoadRaw("Internal/sh_Vertex_Input.h"), false) + "\n";

                    code = internalIncludes + code;

                    if (Type == ShaderPartType.Vertex)
                    {
                        string backbufferCode = loadFile(store.LoadRaw("Internal/sh_Vertex_Output.h"), false);

                        if (!string.IsNullOrEmpty(backbufferCode))
                        {
                            string realMainName = "real_main_" + Guid.NewGuid().ToString("N");

                            backbufferCode = backbufferCode.Replace("{{ real_main }}", realMainName);
                            code = Regex.Replace(code, @"void main\((.*)\)", $"void {realMainName}()") + backbufferCode + '\n';
                        }
                    }
                }

                return code;
            }
        }

        public string GetRawText() => string.Join('\n', shaderCodes);

        #region IDisposable Support

        public void Dispose()
        {
        }

        #endregion
    }
}
