// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using Veldrid;
using PrimitiveTopology = Veldrid.PrimitiveTopology;
using StencilOperation = Veldrid.StencilOperation;

namespace osu.Framework.Graphics.Veldrid
{
    internal static class VeldridExtensions
    {
        public static RgbaFloat ToRgbaFloat(this Color4 colour) => new RgbaFloat(colour.R, colour.G, colour.B, colour.A);

        // todo: ColorWriteMask is necessary for front-to-back render support.
        // public static BlendAttachmentDescription ToBlendAttachment(this BlendingParameters parameters, ColorWriteMask writeMask = ColorWriteMask.All) => new BlendAttachmentDescription
        public static BlendAttachmentDescription ToBlendAttachment(this BlendingParameters parameters) => new BlendAttachmentDescription
        {
            BlendEnabled = !parameters.IsDisabled,
            SourceColorFactor = parameters.Source.ToBlendFactor(),
            SourceAlphaFactor = parameters.SourceAlpha.ToBlendFactor(),
            DestinationColorFactor = parameters.Destination.ToBlendFactor(),
            DestinationAlphaFactor = parameters.DestinationAlpha.ToBlendFactor(),
            ColorFunction = parameters.RGBEquation.ToBlendFunction(),
            AlphaFunction = parameters.AlphaEquation.ToBlendFunction(),
            // ColorWriteMask = writeMask,
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

        public static SamplerFilter ToSamplerFilter(this TextureFilteringMode mode)
        {
            switch (mode)
            {
                case TextureFilteringMode.Linear:
                    return SamplerFilter.MinLinear_MagLinear_MipLinear;

                case TextureFilteringMode.Nearest:
                    return SamplerFilter.MinPoint_MagPoint_MipPoint;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public static ComparisonKind ToComparisonKind(this BufferTestFunction function)
        {
            switch (function)
            {
                case BufferTestFunction.Always:
                    return ComparisonKind.Always;

                case BufferTestFunction.Never:
                    return ComparisonKind.Never;

                case BufferTestFunction.LessThan:
                    return ComparisonKind.Less;

                case BufferTestFunction.Equal:
                    return ComparisonKind.Equal;

                case BufferTestFunction.LessThanOrEqual:
                    return ComparisonKind.LessEqual;

                case BufferTestFunction.GreaterThan:
                    return ComparisonKind.Greater;

                case BufferTestFunction.NotEqual:
                    return ComparisonKind.NotEqual;

                case BufferTestFunction.GreaterThanOrEqual:
                    return ComparisonKind.GreaterEqual;

                default:
                    throw new ArgumentOutOfRangeException(nameof(function));
            }
        }

        public static StencilOperation ToStencilOperation(this Rendering.StencilOperation operation)
        {
            switch (operation)
            {
                case Rendering.StencilOperation.Zero:
                    return StencilOperation.Zero;

                case Rendering.StencilOperation.Invert:
                    return StencilOperation.Invert;

                case Rendering.StencilOperation.Keep:
                    return StencilOperation.Keep;

                case Rendering.StencilOperation.Replace:
                    return StencilOperation.Replace;

                case Rendering.StencilOperation.Increase:
                    return StencilOperation.IncrementAndClamp;

                case Rendering.StencilOperation.Decrease:
                    return StencilOperation.DecrementAndClamp;

                case Rendering.StencilOperation.IncreaseWrap:
                    return StencilOperation.IncrementAndWrap;

                case Rendering.StencilOperation.DecreaseWrap:
                    return StencilOperation.DecrementAndWrap;

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }

        public static VertexElementFormat ToVertexElementFormat(this VertexAttribPointerType type, int count)
        {
            switch (type)
            {
                case VertexAttribPointerType.Byte when count == 2:
                    return VertexElementFormat.SByte2;

                case VertexAttribPointerType.Byte when count == 4:
                    return VertexElementFormat.SByte4;

                case VertexAttribPointerType.UnsignedByte when count == 2:
                    return VertexElementFormat.Byte2;

                case VertexAttribPointerType.UnsignedByte when count == 4:
                    return VertexElementFormat.Byte4;

                case VertexAttribPointerType.Short when count == 2:
                    return VertexElementFormat.Short2;

                case VertexAttribPointerType.Short when count == 4:
                    return VertexElementFormat.Short4;

                case VertexAttribPointerType.UnsignedShort when count == 2:
                    return VertexElementFormat.UShort2;

                case VertexAttribPointerType.UnsignedShort when count == 4:
                    return VertexElementFormat.UShort4;

                case VertexAttribPointerType.Int when count == 1:
                    return VertexElementFormat.Int1;

                case VertexAttribPointerType.Int when count == 2:
                    return VertexElementFormat.Int2;

                case VertexAttribPointerType.Int when count == 3:
                    return VertexElementFormat.Int3;

                case VertexAttribPointerType.Int when count == 4:
                    return VertexElementFormat.Int4;

                case VertexAttribPointerType.UnsignedInt when count == 1:
                    return VertexElementFormat.UInt1;

                case VertexAttribPointerType.UnsignedInt when count == 2:
                    return VertexElementFormat.UInt2;

                case VertexAttribPointerType.UnsignedInt when count == 3:
                    return VertexElementFormat.UInt3;

                case VertexAttribPointerType.UnsignedInt when count == 4:
                    return VertexElementFormat.UInt4;

                case VertexAttribPointerType.Float when count == 1:
                    return VertexElementFormat.Float1;

                case VertexAttribPointerType.Float when count == 2:
                    return VertexElementFormat.Float2;

                case VertexAttribPointerType.Float when count == 3:
                    return VertexElementFormat.Float3;

                case VertexAttribPointerType.Float when count == 4:
                    return VertexElementFormat.Float4;

                case VertexAttribPointerType.HalfFloat when count == 1:
                    return VertexElementFormat.Half1;

                case VertexAttribPointerType.HalfFloat when count == 2:
                    return VertexElementFormat.Half2;

                case VertexAttribPointerType.HalfFloat when count == 4:
                    return VertexElementFormat.Half4;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static PrimitiveTopology ToPrimitiveTopology(this Rendering.PrimitiveTopology type)
        {
            switch (type)
            {
                case Rendering.PrimitiveTopology.Points:
                    return PrimitiveTopology.PointList;

                case Rendering.PrimitiveTopology.Lines:
                    return PrimitiveTopology.LineList;

                case Rendering.PrimitiveTopology.LineStrip:
                    return PrimitiveTopology.LineStrip;

                case Rendering.PrimitiveTopology.Triangles:
                    return PrimitiveTopology.TriangleList;

                case Rendering.PrimitiveTopology.TriangleStrip:
                    return PrimitiveTopology.TriangleStrip;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
