// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using osu.Framework.Development;
using osu.Framework.Logging;
using osu.Framework.Platform;
using SharpGen.Runtime;
using Veldrid;
using Veldrid.OpenGLBinding;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vulkan;

namespace osu.Framework.Graphics.Veldrid
{
    public static partial class Vd
    {
        private static void initialiseDevice(GameHost host)
        {
            var options = new GraphicsDeviceOptions
            {
                HasMainSwapchain = true,
                SwapchainDepthFormat = null,
                // SwapchainSrgbFormat = false,
                SyncToVerticalBlank = true,
                PreferDepthRangeZeroToOne = true,
                PreferStandardClipSpaceYDirection = true,
                ResourceBindingModel = ResourceBindingModel.Improved,
                // todo: should probably be removed, we don't want validation layers to be coupled with debug config.
                Debug = DebugUtils.IsDebugBuild,
            };

            Device = createDevice(host.Window, options);

            unsafe
            {
                switch (host.Window.Graphics.Type)
                {
                    case GraphicsBackend.OpenGL:
                    case GraphicsBackend.OpenGLES:
                    {
                        string version = default;
                        string renderer = default;
                        string glslVersion = default;
                        string vendor = default;

                        StringBuilder extensions = default;

                        Device.GetOpenGLInfo().ExecuteOnGLThread(() =>
                        {
                            version = Marshal.PtrToStringUTF8((IntPtr)OpenGLNative.glGetString(StringName.Version));
                            renderer = Marshal.PtrToStringUTF8((IntPtr)OpenGLNative.glGetString(StringName.Renderer));
                            vendor = Marshal.PtrToStringUTF8((IntPtr)OpenGLNative.glGetString(StringName.Vendor));
                            glslVersion = Marshal.PtrToStringUTF8((IntPtr)OpenGLNative.glGetString(StringName.ShadingLanguageVersion));

                            int extensionCount;
                            OpenGLNative.glGetIntegerv(GetPName.NumExtensions, &extensionCount);

                            extensions = new StringBuilder();

                            for (uint i = 0; i < extensionCount; i++)
                            {
                                if (i > 0)
                                    extensions.Append(' ');

                                extensions.Append(Marshal.PtrToStringUTF8((IntPtr)OpenGLNative.glGetStringi(StringNameIndexed.Extensions, i)));
                            }

                            int maxTextureSize;
                            OpenGLNative.glGetIntegerv(GetPName.MaxTextureSize, &maxTextureSize);
                            MaxTextureSize = maxTextureSize;
                        });

                        Logger.Log($@"GL Initialized
                                    GL Version:                 {version}
                                    GL Renderer:                {renderer}
                                    GL Shader Language version: {glslVersion}
                                    GL Vendor:                  {vendor}
                                    GL Extensions:              {extensions}");

                        break;
                    }

                    case GraphicsBackend.Vulkan:
                    {
                        var info = Device.GetVulkanInfo();
                        var device = info.PhysicalDevice;

                        uint instanceExtensionsCount = 0;
                        VulkanNative.vkEnumerateInstanceExtensionProperties(string.Empty, ref instanceExtensionsCount, IntPtr.Zero);

                        var instanceExtensions = new VkExtensionProperties[(int)instanceExtensionsCount];
                        VulkanNative.vkEnumerateInstanceExtensionProperties(string.Empty, ref instanceExtensionsCount, ref instanceExtensions[0]);

                        uint deviceExetnsionsCount = 0;
                        VulkanNative.vkEnumerateDeviceExtensionProperties(device, string.Empty, ref deviceExetnsionsCount, IntPtr.Zero);

                        var deviceExtensions = new VkExtensionProperties[(int)deviceExetnsionsCount];
                        VulkanNative.vkEnumerateDeviceExtensionProperties(device, string.Empty, ref deviceExetnsionsCount, ref deviceExtensions[0]);

                        VkPhysicalDeviceProperties properties;
                        VulkanNative.vkGetPhysicalDeviceProperties(device, &properties);

                        MaxTextureSize = (int)properties.limits.maxImageDimension2D;

                        string backend = RuntimeInfo.IsApple ? "MoltenVK" : "Vulkan";
                        string extensions = string.Join(" ", instanceExtensions.Concat(deviceExtensions).Select(e => Marshal.PtrToStringUTF8((IntPtr)e.extensionName)));

                        string apiVersion = $"{properties.apiVersion >> 22}.{(properties.apiVersion >> 12) & 0x3FFU}.{properties.apiVersion & 0xFFFU}";
                        string driverVersion;

                        // https://github.com/SaschaWillems/vulkan.gpuinfo.org/blob/1e6ca6e3c0763daabd6a101b860ab4354a07f5d3/functions.php#L293-L325
                        if (properties.vendorID == 0x10DE) // NVIDIA's versioning convention
                            driverVersion = $"{properties.driverVersion >> 22}.{(properties.driverVersion >> 14) & 0x0FFU}.{(properties.driverVersion >> 6) & 0x0FFU}.{properties.driverVersion & 0x003U}";
                        else if (properties.vendorID == 0x8086 && RuntimeInfo.OS == RuntimeInfo.Platform.Windows) // Intel's versioning convention on Windows
                            driverVersion = $"{properties.driverVersion >> 22}.{properties.driverVersion & 0x3FFFU}";
                        else // Vulkan's convention
                            driverVersion = $"{properties.driverVersion >> 22}.{(properties.driverVersion >> 12) & 0x3FFU}.{properties.driverVersion & 0xFFFU}";

                        Logger.Log($@"{backend} Initialized
                                    {backend} API Version:    {apiVersion}
                                    {backend} Driver Version: {driverVersion}
                                    {backend} Device:         {Marshal.PtrToStringUTF8((IntPtr)properties.deviceName)}
                                    {backend} Extensions:     {extensions}");
                        break;
                    }

                    case GraphicsBackend.Direct3D11:
                    {
                        var info = Device.GetD3D11Info();
                        var adapter = MarshallingHelpers.FromPointer<IDXGIAdapter>(info.Adapter);
                        var device = MarshallingHelpers.FromPointer<ID3D11Device>(info.Device);

                        MaxTextureSize = ID3D11Resource.MaximumTexture2DSize;

                        Logger.Log($@"Direct3D 11 Initialized
                                    Direct3D 11 Feature Level:           {device.FeatureLevel.ToString().Replace("Level_", string.Empty).Replace("_", ".")}
                                    Direct3D 11 Adapter:                 {adapter.Description.Description}
                                    Direct3D 11 Dedicated Video Memory:  {adapter.Description.DedicatedVideoMemory / 1024 / 1024} MB
                                    Direct3D 11 Dedicated System Memory: {adapter.Description.DedicatedSystemMemory / 1024 / 1024} MB
                                    Direct3D 11 Shared System Memory:    {adapter.Description.SharedSystemMemory / 1024 / 1024} MB");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="GraphicsDevice"/> based off the <see cref="IWindow"/> specified.
        /// </summary>
        /// <param name="window">The <see cref="IWindow"/> to create a graphics device for.</param>
        /// <param name="options">The graphics device options.</param>
        private static GraphicsDevice createDevice(IWindow window, GraphicsDeviceOptions options)
        {
            var swapchainDescription = new SwapchainDescription
            {
                Width = (uint)window.ClientSize.Width,
                Height = (uint)window.ClientSize.Height,
                ColorSrgb = options.SwapchainSrgbFormat,
                DepthFormat = options.SwapchainDepthFormat,
                SyncToVerticalBlank = options.SyncToVerticalBlank,
            };

            var type = window.Graphics.Type;

            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    swapchainDescription.Source = SwapchainSource.CreateWin32(window.WindowHandle, IntPtr.Zero);
                    break;

                case RuntimeInfo.Platform.macOS:
                    if (type == GraphicsBackend.Vulkan)
                    {
                        // Vulkan's validation layer is busted with Veldrid on macOS.
                        // todo: remove once https://github.com/mellinoe/veldrid/pull/419 is merged.
                        options.Debug = false;
                    }

                    swapchainDescription.Source = SwapchainSource.CreateNSWindow(window.WindowHandle);
                    break;

                case RuntimeInfo.Platform.Linux:
                    swapchainDescription.Source = SwapchainSource.CreateXlib(window.DisplayHandle, window.WindowHandle);
                    break;
            }

            switch (type)
            {
                case GraphicsBackend.OpenGL:
                    if (!(window.Graphics is IHasOpenGLCapability openGLBackend))
                        throw new InvalidOperationException($"Attempting to run under OpenGL while window graphics backend does not implement {nameof(IHasOpenGLCapability)}.");

                    openGLBackend.PrepareOpenGL(out var openGLPlatformInfo);
                    return GraphicsDevice.CreateOpenGL(options, openGLPlatformInfo, swapchainDescription.Width, swapchainDescription.Height);

                case GraphicsBackend.OpenGLES:
                    return GraphicsDevice.CreateOpenGLES(options, swapchainDescription);

                case GraphicsBackend.Direct3D11:
                    return GraphicsDevice.CreateD3D11(options, swapchainDescription);

                case GraphicsBackend.Vulkan:
                    return GraphicsDevice.CreateVulkan(options, swapchainDescription);

                case GraphicsBackend.Metal:
                    return GraphicsDevice.CreateMetal(options, swapchainDescription);
            }

            return null;
        }
    }
}
