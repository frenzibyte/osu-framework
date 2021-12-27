// // Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// // See the LICENCE file in the repository root for full licence text.
//
// using osu.Framework.Graphics.Renderer.Textures;
// using Veldrid;
//
// namespace osu.Framework.Platform.SDL2
// {
//     public class VeldridTextureResourceSet<T> : VeldridResourceSet
//         where T : BindableResource
//     {
//         public VeldridTextureResourceSet()
//             : base(new )
//         {
//         }
//
//         public void SetResource(TextureUnit unit, T resource) => SetResource((int)unit, resource);
//
//         protected override ResourceLayoutElementDescription CreateResourceLayoutElementFor(int index) => new ResourceLayoutElementDescription
//         {
//             Name = $"m_{typeof(T).Name}{index}",
//             Kind = kind,
//             Stages = ShaderStages.Fragment,
//         };
//     }
// }
