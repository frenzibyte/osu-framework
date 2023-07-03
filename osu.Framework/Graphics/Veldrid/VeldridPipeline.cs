// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridPipeline
    {
        private bool pipelineValid = true;
        private bool resourcesValid = true;

        private readonly VeldridRenderer renderer;
        private GraphicsPipelineDescription pipelineDescription;

        private readonly Dictionary<GraphicsPipelineDescription, Pipeline> pipelineCache = new Dictionary<GraphicsPipelineDescription, Pipeline>();

        private readonly Dictionary<int, VeldridTextureResources> boundTextureUnits = new Dictionary<int, VeldridTextureResources>();
        private readonly Dictionary<string, IVeldridUniformBuffer> boundUniformBuffers = new Dictionary<string, IVeldridUniformBuffer>();
        private VeldridShader? boundShader;

        private static readonly GlobalStatistic<int> stat_graphics_pipeline_updates = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Pipeline updates");
        private static readonly GlobalStatistic<int> stat_graphics_pipeline_instances = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Pipeline instances");

        public VeldridPipeline(VeldridRenderer renderer, GraphicsPipelineDescription pipelineDescription)
        {
            this.renderer = renderer;
            this.pipelineDescription = pipelineDescription;
        }

        public void NewFrame()
        {
            stat_graphics_pipeline_updates.Value = 0;

            pipelineValid = false;
            resourcesValid = false;
        }

        public void UpdateState(Func<GraphicsPipelineDescription, GraphicsPipelineDescription> action)
        {
            var pipelineBefore = pipelineDescription;
            pipelineDescription = action(pipelineDescription);
            pipelineValid &= pipelineDescription.Equals(pipelineBefore);
        }

        public void PrepareForDraw()
        {
            Debug.Assert(boundShader != null);

            if (!resourcesValid)
            {
                Array.Resize(ref pipelineDescription.ResourceLayouts, boundShader.LayoutCount);

                // Activate texture layouts.
                foreach (var (unit, _) in boundTextureUnits)
                {
                    var layout = boundShader.GetTextureLayout(unit);
                    if (layout == null)
                        continue;

                    pipelineDescription.ResourceLayouts[layout.Set] = layout.Layout;
                }

                // Activate uniform buffer layouts.
                foreach (var (name, _) in boundUniformBuffers)
                {
                    var layout = boundShader.GetUniformBufferLayout(name);
                    if (layout == null)
                        continue;

                    pipelineDescription.ResourceLayouts[layout.Set] = layout.Layout;
                }
            }

            if (!pipelineValid)
            {
                if (!pipelineCache.TryGetValue(pipelineDescription, out var pipeline))
                {
                    pipelineCache[pipelineDescription.Clone()] = pipeline = renderer.Factory.CreateGraphicsPipeline(ref pipelineDescription);
                    stat_graphics_pipeline_instances.Value++;
                }

                renderer.Commands.SetPipeline(pipeline);
                stat_graphics_pipeline_updates.Value++;

                pipelineValid = true;
            }

            if (!resourcesValid)
            {
                // Activate texture resources.
                foreach (var (unit, texture) in boundTextureUnits)
                {
                    var layout = boundShader.GetTextureLayout(unit);
                    if (layout == null)
                        continue;

                    renderer.Commands.SetGraphicsResourceSet((uint)layout.Set, texture.GetResourceSet(renderer, layout.Layout));
                }

                // Activate uniform buffer resources.
                foreach (var (name, buffer) in boundUniformBuffers)
                {
                    var layout = boundShader.GetUniformBufferLayout(name);
                    if (layout == null)
                        continue;

                    renderer.Commands.SetGraphicsResourceSet((uint)layout.Set, buffer.GetResourceSet(layout.Layout));
                }
            }
        }

        public void BindTextureResource(VeldridTextureResources texture, int unit)
        {
            if (boundTextureUnits.TryGetValue(unit, out var existing) && texture == existing)
                return;

            boundTextureUnits[unit] = texture;
            resourcesValid = false;
        }

        public void BindUniformBuffer(IVeldridUniformBuffer uniform, string name)
        {
            if (boundUniformBuffers.TryGetValue(name, out var existing) && uniform == existing)
                return;

            boundUniformBuffers[name] = uniform;
            resourcesValid = false;
        }

        public void BindShader(VeldridShader shader)
        {
            boundShader = shader;
            pipelineDescription.ShaderSet.Shaders = shader.Shaders;

            pipelineValid = false;
            resourcesValid = false;
        }
    }
}
