// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Veldrid;
using Encoding = System.Text.Encoding;
using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;

namespace osu.Framework.Graphics.Shaders
{
    internal class ShaderPart
    {
        internal string Name { get; }

        internal ShaderStages Type { get; }

        internal IReadOnlyList<ShaderUniformInfo> Uniforms { get; }

        private readonly string code;

        private static readonly Regex include_regex = new Regex("^\\s*#\\s*include\\s+[\"<](.*)[\">]");
        private static readonly Regex shader_attribute_location_regex = new Regex(@"(layout\(location\s=\s)(-?\d+)(\)\s(?>attribute|in).*)", RegexOptions.Multiline);
        private static readonly Regex shader_resource_regex = new Regex(@"^\s*(?>uniform)\s+(?:lowp|mediump|highp)\s+?(texture2D|sampler)\s+\w+;", RegexOptions.Multiline);
        private static readonly Regex shader_uniform_regex = new Regex(@"^\s*(?>uniform)\s+(?:(lowp|mediump|highp)\s+)?(\w+)\s+(\w+);", RegexOptions.Multiline);

        private ShaderPart(string name, ShaderStages type, string code, List<ShaderUniformInfo> uniforms)
        {
            Name = name;
            Type = type;
            Uniforms = uniforms;

            this.code = code;
        }

        internal static ShaderPart LoadFromFile(string name, byte[] data, ShaderStages type, ShaderManager manager)
        {
            var uniforms = new List<ShaderUniformInfo>();

            string code = loadFile(data, true, type, manager, uniforms);

            return new ShaderPart(name, type, code, uniforms);
        }

        private static string loadFile(byte[] data, bool mainFile, ShaderStages type, ShaderManager manager, List<ShaderUniformInfo> uniforms)
        {
            if (data == null)
                return null;

            using (MemoryStream ms = new MemoryStream(data))
            using (StreamReader sr = new StreamReader(ms))
            {
                string code = string.Empty;

                while (sr.Peek() != -1)
                {
                    string line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line))
                        continue;

                    Match includeMatch = include_regex.Match(line);

                    if (includeMatch.Success)
                    {
                        string includeName = includeMatch.Groups[1].Value.Trim();

                        code += loadFile(manager.LoadRaw(includeName), false, type, manager, uniforms) + '\n';
                    }
                    else
                        code += line + '\n';
                }

                if (!mainFile)
                    return code;

                code = loadFile(manager.LoadRaw("sh_Precision_Internal.h"), false, type, manager, uniforms) + "\n" + code;

                if (type == ShaderStages.Vertex)
                    code = appendBackbuffer(code, manager, uniforms);

                code = resolveResources(code);
                code = emitUniforms(code, uniforms);

                return code;
            }
        }

        private static string appendBackbuffer(string code, ShaderManager manager, List<ShaderUniformInfo> uniforms)
        {
            string realMainName = "real_main_" + Guid.NewGuid().ToString("N");

            string backbufferCode = loadFile(manager.LoadRaw("sh_Backbuffer_Internal.h"), false, ShaderStages.Vertex, manager, uniforms);

            Match backbufferLocationMatch = shader_attribute_location_regex.Match(backbufferCode);
            int backbufferAttributeOffset = -int.Parse(backbufferLocationMatch.Groups[2].Value);

            backbufferCode = backbufferCode.Replace("{{ real_main }}", realMainName);
            code = Regex.Replace(code, @"void main\((.*)\)", $"void {realMainName}()") + backbufferCode + '\n';

            Match attributeLocationMatch = shader_attribute_location_regex.Match(code);

            while (attributeLocationMatch.Success)
            {
                int.TryParse(attributeLocationMatch.Groups[2].Value, out int location);
                code = code.Replace(attributeLocationMatch.Value, shader_attribute_location_regex.Replace(attributeLocationMatch.Value, m => m.Groups[1] + $"{location + backbufferAttributeOffset}" + m.Groups[3]));

                attributeLocationMatch = attributeLocationMatch.NextMatch();
            }

            return code;
        }

        private static string resolveResources(string code)
        {
            Match resourceMatch = shader_resource_regex.Match(code);

            while (resourceMatch.Success)
            {
                ResourceKind kind;

                switch (resourceMatch.Groups[1].Value)
                {
                    case "texture2D":
                        kind = ResourceKind.TextureReadOnly;
                        break;

                    case "sampler":
                        kind = ResourceKind.Sampler;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(code));
                }

                Vd.ResourceSet.GetLayout(kind, out int index);
                code = code.Replace(resourceMatch.Value, $"layout(binding = {index}) {resourceMatch.Value}");

                resourceMatch = resourceMatch.NextMatch();
            }

            return code;
        }

        private static string emitUniforms(string code, List<ShaderUniformInfo> uniforms)
        {
            Match uniformMatch = shader_uniform_regex.Match(code);

            if (!uniformMatch.Success)
                return code;

            do
            {
                ShaderUniformInfo info = new ShaderUniformInfo
                {
                    Name = uniformMatch.Groups[3].Value.Trim(),
                    Type = uniformMatch.Groups[2].Value.Trim(),
                    Precision = uniformMatch.Groups[1].Value.Trim(),
                };

                uniforms.Add(info);
                code = code.Replace(uniformMatch.Value, string.Empty);
            } while ((uniformMatch = uniformMatch.NextMatch()).Success);

            return code;
        }

        private string includeUniformStructure(string code, IReadOnlyList<ShaderUniformInfo> uniforms)
        {
            if (uniforms.Count == 0)
                return code;

            var uniformLayout = Vd.ResourceSet.GetLayout(ResourceKind.UniformBuffer, out int uniformIndex);

            var uniformBuilder = new StringBuilder();
            uniformBuilder.AppendLine($"layout(std140, binding = {uniformIndex}) uniform {uniformLayout.Name}");
            uniformBuilder.AppendLine("{");

            foreach (var uniform in uniforms)
                uniformBuilder.AppendLine($"{uniform.Precision} {uniform.Type} {uniform.Name};".Trim());

            uniformBuilder.AppendLine("};");
            uniformBuilder.AppendLine();

            return $"{uniformBuilder}{code}";
        }

        internal byte[] GetData(IReadOnlyList<ShaderUniformInfo> uniforms)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Array.Empty<byte>();

            string result = includeUniformStructure(code, uniforms);

            if (!result.StartsWith("#version", StringComparison.Ordinal))
                result = $"#version 450\n{result}";

            return Encoding.UTF8.GetBytes(result);
        }
    }
}
