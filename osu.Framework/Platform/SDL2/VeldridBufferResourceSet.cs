// // Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// // See the LICENCE file in the repository root for full licence text.
//
// using System.Diagnostics;
// using Veldrid;
//
// namespace osu.Framework.Platform.SDL2
// {
//     public class VeldridBufferResourceSet : VeldridResourceSet
//     {
//         public string BufferName { get; }
//
//         public ResourceKind BufferKind { get; }
//
//         public VeldridBufferResourceSet(int index, string bufferName, ResourceKind bufferKind)
//             : base(index)
//         {
//             BufferName = bufferName;
//             BufferKind = bufferKind;
//
//             Debug.Assert(bufferKind == ResourceKind.UniformBuffer || bufferKind == ResourceKind.StructuredBufferReadOnly || bufferKind == ResourceKind.StructuredBufferReadWrite);
//         }
//
//         public void SetBuffer(DeviceBuffer buffer) => SetResource(0, buffer);
//
//         protected override ResourceLayoutElementDescription CreateResourceLayoutElementFor(int index) => new ResourceLayoutElementDescription
//         {
//             Name = BufferName,
//             Kind = BufferKind,
//             Stages = ShaderStages.Fragment | ShaderStages.Vertex,
//         };
//     }
// }
