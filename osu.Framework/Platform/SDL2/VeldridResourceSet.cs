// // Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// // See the LICENCE file in the repository root for full licence text.
//
// using System;
// using Veldrid;
// using Vd = osu.Framework.Platform.SDL2.VeldridGraphicsBackend;
//
// namespace osu.Framework.Platform.SDL2
// {
//     public abstract class VeldridResourceSet
//     {
//         private ResourceLayoutDescription layoutDescription;
//         private ResourceSetDescription setDescription;
//
//         private ResourceSet resourceSet;
//
//         /// <summary>
//         /// The index of the resource set.
//         /// </summary>
//         public int Index { get; }
//
//         /// <summary>
//         /// The resource layout of the resource set.
//         /// </summary>
//         public ResourceLayout Layout { get; private set; }
//
//         protected VeldridResourceSet(int index)
//         {
//             Index = index;
//
//             layoutDescription = new ResourceLayoutDescription(Array.Empty<ResourceLayoutElementDescription>());
//             setDescription = new ResourceSetDescription(null);
//         }
//
//         private bool layoutRequiresReinstantiation;
//         private bool setRequiresReinstantiation;
//
//         /// <summary>
//         /// Sets a <see cref="BindableResource"/> to the specified index.
//         /// </summary>
//         /// <param name="index">The index to set the resource at.</param>
//         /// <param name="resource">The resource.</param>
//         protected void SetResource(int index, BindableResource resource)
//         {
//             setDescription.BoundResources[index] = resource;
//             setRequiresReinstantiation = true;
//         }
//
//         /// <summary>
//         /// Re-instantiates the resource set and layout, if required.
//         /// </summary>
//         public void Refresh()
//         {
//             if (!setRequiresReinstantiation)
//                 return;
//
//             resourceSet?.Dispose();
//
//             if (layoutRequiresReinstantiation)
//             {
//                 Layout?.Dispose();
//                 Layout = Vd.Factory.CreateResourceLayout(layoutDescription);
//                 setDescription.Layout = Layout;
//
//                 layoutRequiresReinstantiation = false;
//             }
//
//             resourceSet = Vd.Factory.CreateResourceSet(setDescription);
//             setRequiresReinstantiation = false;
//         }
//
//         public static implicit operator ResourceSet(VeldridResourceSet wrapper)
//         {
//             wrapper.Refresh();
//             return wrapper.resourceSet;
//         }
//
//         /// <summary>
//         /// Creates a <see cref="ResourceLayoutElementDescription"/> for the provided layout index.
//         /// </summary>
//         /// <param name="index">The index of the element.</param>
//         protected abstract ResourceLayoutElementDescription CreateResourceLayoutElementFor(int index);
//     }
// }
