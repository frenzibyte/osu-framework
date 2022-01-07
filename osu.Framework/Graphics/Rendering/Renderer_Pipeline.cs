// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Platform.Graphics;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Rendering
{
    public partial class Renderer
    {
        #region Scissor Test

        private static readonly Stack<bool> scissor_state_stack = new Stack<bool>();

        private static bool currentScissorState;

        public static void PushScissorState(bool enabled)
        {
            scissor_state_stack.Push(enabled);
            setScissorState(enabled);
        }

        public static void PopScissorState()
        {
            Trace.Assert(scissor_state_stack.Count > 1);

            scissor_state_stack.Pop();

            setScissorState(scissor_state_stack.Peek());
        }

        private static void setScissorState(bool enabled)
        {
            if (enabled == currentScissorState)
                return;

            currentScissorState = enabled;

            pipelineDescription.RasterizerState.ScissorTestEnabled = enabled;
        }

        #endregion

        #region Blending

        private static BlendingParameters lastBlendingParameters;

        /// <summary>
        /// Sets the blending function to draw with.
        /// </summary>
        /// <param name="blendingParameters">The info we should use to update the active state.</param>
        public static void SetBlend(BlendingParameters blendingParameters)
        {
            if (lastBlendingParameters == blendingParameters)
                return;

            FlushCurrentBatch();

            pipelineDescription.BlendState = new BlendStateDescription(default, blendingParameters.ToBlendAttachment());
            lastBlendingParameters = blendingParameters;
        }

        #endregion

        #region Depth

        /// <summary>
        /// Applies a new depth information.
        /// </summary>
        /// <param name="depthInfo">The depth information.</param>
        public static void PushDepthInfo(DepthInfo depthInfo)
        {
            depth_stack.Push(depthInfo);

            if (CurrentDepthInfo.Equals(depthInfo))
                return;

            CurrentDepthInfo = depthInfo;
            setDepthInfo(CurrentDepthInfo);
        }

        /// <summary>
        /// Applies the last depth information.
        /// </summary>
        public static void PopDepthInfo()
        {
            Trace.Assert(depth_stack.Count > 1);

            depth_stack.Pop();
            DepthInfo depthInfo = depth_stack.Peek();

            if (CurrentDepthInfo.Equals(depthInfo))
                return;

            CurrentDepthInfo = depthInfo;
            setDepthInfo(CurrentDepthInfo);
        }

        private static void setDepthInfo(DepthInfo depthInfo)
        {
            FlushCurrentBatch();

            pipelineDescription.DepthStencilState.DepthTestEnabled = depthInfo.DepthTest;
            pipelineDescription.DepthStencilState.DepthWriteEnabled = depthInfo.WriteDepth;
            pipelineDescription.DepthStencilState.DepthComparison = depthInfo.Function;
        }

        #endregion

        private static readonly GlobalStatistic<int> stat_graphics_pipeline_created = GlobalStatistics.Get<int>("Renderer", "Graphics pipelines created");

        private static readonly Dictionary<GraphicsPipelineDescription, Pipeline> pipeline_cache = new Dictionary<GraphicsPipelineDescription, Pipeline>();

        private static Pipeline fetchPipeline(GraphicsPipelineDescription description)
        {
            if (!pipeline_cache.TryGetValue(description, out var pipeline))
            {
                pipeline_cache[description.Clone()] = pipeline = Factory.CreateGraphicsPipeline(description);
                stat_graphics_pipeline_created.Value++;
            }

            return pipeline;
        }
    }
}
