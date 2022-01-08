// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering.Textures;
using Veldrid;
using BlendStateDescription = Veldrid.BlendStateDescription;
using PixelFormat = osu.Framework.Graphics.Rendering.Textures.PixelFormat;
using PrimitiveTopology = osu.Framework.Graphics.Rendering.PrimitiveTopology;
using VdPixelFormat = Veldrid.PixelFormat;
using VdPrimitiveTopology = Veldrid.PrimitiveTopology;

namespace osu.Framework.Platform.Graphics
{
    public static class VeldridExtensions
    {
        public static RgbaFloat ToRgbaFloat(this Colour4 colour) => new RgbaFloat(colour.R, colour.G, colour.B, colour.A);

        public static BlendAttachmentDescription ToBlendAttachment(this BlendingParameters parameters) => new BlendAttachmentDescription
        {
            BlendEnabled = !parameters.IsDisabled,
            SourceColorFactor = parameters.Source.ToBlendFactor(),
            SourceAlphaFactor = parameters.SourceAlpha.ToBlendFactor(),
            DestinationColorFactor = parameters.Destination.ToBlendFactor(),
            DestinationAlphaFactor = parameters.DestinationAlpha.ToBlendFactor(),
            ColorFunction = parameters.RGBEquation.ToBlendFunction(),
            AlphaFunction = parameters.AlphaEquation.ToBlendFunction(),
        };

        public static BlendFactor ToBlendFactor(this BlendingType type)
        {
            switch (type)
            {
                case BlendingType.DstAlpha:
                    return BlendFactor.DestinationAlpha;

                case BlendingType.DstColor:
                    return BlendFactor.DestinationColor;

                case BlendingType.SrcAlpha:
                    return BlendFactor.SourceAlpha;

                case BlendingType.SrcColor:
                    return BlendFactor.SourceColor;

                case BlendingType.OneMinusDstAlpha:
                    return BlendFactor.InverseDestinationAlpha;

                case BlendingType.OneMinusDstColor:
                    return BlendFactor.InverseDestinationColor;

                case BlendingType.OneMinusSrcAlpha:
                    return BlendFactor.InverseSourceAlpha;

                case BlendingType.OneMinusSrcColor:
                    return BlendFactor.InverseSourceColor;

                case BlendingType.One:
                    return BlendFactor.One;

                case BlendingType.Zero:
                    return BlendFactor.Zero;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public static BlendFunction ToBlendFunction(this BlendingEquation equation)
        {
            switch (equation)
            {
                case BlendingEquation.Add:
                    return BlendFunction.Add;

                case BlendingEquation.Subtract:
                    return BlendFunction.Subtract;

                case BlendingEquation.ReverseSubtract:
                    return BlendFunction.ReverseSubtract;

                case BlendingEquation.Min:
                    return BlendFunction.Minimum;

                case BlendingEquation.Max:
                    return BlendFunction.Maximum;

                default:
                    throw new ArgumentOutOfRangeException(nameof(equation));
            }
        }

        public static VdPixelFormat ToPixelFormat(this PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.RGBA_Srgb:
                    return VdPixelFormat.R8_G8_B8_A8_UNorm_SRgb;

                case PixelFormat.BGRA:
                    return VdPixelFormat.B8_G8_R8_A8_UNorm;

                case PixelFormat.Red:
                    return VdPixelFormat.R8_UNorm;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        public static SamplerFilter ToSamplerFilter(this FilteringMode mode, bool manualMipmaps = true)
        {
            // var minFilter = manualMipmaps ? filteringMode : (filteringMode == FilteringMode.Linear ? FilteringMode.LinearMipmapLinear : FilteringMode.Nearest);

            switch (mode)
            {
                case FilteringMode.Linear:
                    return SamplerFilter.MinLinear_MagLinear_MipLinear;

                case FilteringMode.Nearest:
                    return SamplerFilter.MinPoint_MagPoint_MipPoint;

                case FilteringMode.LinearMipmapNearest:
                    return manualMipmaps
                        ? SamplerFilter.MinLinear_MagLinear_MipPoint
                        : SamplerFilter.MinPoint_MagPoint_MipPoint;

                case FilteringMode.NearestMipmapLinear:
                    return manualMipmaps
                        ? SamplerFilter.MinPoint_MagLinear_MipLinear
                        : SamplerFilter.MinPoint_MagPoint_MipPoint;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public static VdPrimitiveTopology ToPrimitiveTopology(this PrimitiveTopology topology)
        {
            switch (topology)
            {
                case PrimitiveTopology.Triangles:
                    return VdPrimitiveTopology.TriangleList;

                case PrimitiveTopology.TriangleStrip:
                    return VdPrimitiveTopology.TriangleStrip;

                case PrimitiveTopology.Lines:
                    return VdPrimitiveTopology.LineList;

                case PrimitiveTopology.LineStrip:
                    return VdPrimitiveTopology.LineStrip;

                case PrimitiveTopology.Points:
                    return VdPrimitiveTopology.PointList;

                default:
                    throw new ArgumentOutOfRangeException(nameof(topology));
            }
        }

        public static GraphicsPipelineDescription Clone(this GraphicsPipelineDescription description) => new GraphicsPipelineDescription
        {
            DepthStencilState = description.DepthStencilState,
            RasterizerState = description.RasterizerState,
            PrimitiveTopology = description.PrimitiveTopology,
            ResourceBindingModel = description.ResourceBindingModel,
            BlendState = description.BlendState.Clone(),
            ShaderSet = description.ShaderSet.Clone(),
            Outputs = description.Outputs.Clone(),
            ResourceLayouts = (ResourceLayout[])description.ResourceLayouts.Clone(),
        };

        public static BlendStateDescription Clone(this BlendStateDescription description) => new BlendStateDescription
        {
            BlendFactor = description.BlendFactor,
            AlphaToCoverageEnabled = description.AlphaToCoverageEnabled,
            AttachmentStates = (BlendAttachmentDescription[])description.AttachmentStates.Clone(),
        };

        public static ShaderSetDescription Clone(this ShaderSetDescription description) => new ShaderSetDescription
        {
            Shaders = (Shader[])description.Shaders.Clone(),
            VertexLayouts = (VertexLayoutDescription[])description.VertexLayouts.Clone(),
            Specializations = (SpecializationConstant[])description.Specializations?.Clone(),
        };

        public static OutputDescription Clone(this OutputDescription description) => new OutputDescription
        {
            DepthAttachment = description.DepthAttachment,
            SampleCount = description.SampleCount,
            ColorAttachments = (OutputAttachmentDescription[])description.ColorAttachments.Clone(),
        };
    }
}
