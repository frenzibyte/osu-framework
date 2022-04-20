// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using SDL2;
using Veldrid;
using Veldrid.OpenGL;

namespace osu.Framework.Platform.SDL2
{
    public class SDL2GraphicsBackend : IGraphicsBackend, IHasOpenGLCapability
    {
        private SDL2DesktopWindow sdlWindow;

        public GraphicsBackend Type
        {
            get
            {
                switch (RuntimeInfo.OS)
                {
                    case RuntimeInfo.Platform.Windows:
                        return GraphicsBackend.Direct3D11;

                    case RuntimeInfo.Platform.macOS:
                    case RuntimeInfo.Platform.iOS:
                        // Veldrid's implementation of Metal is not really on par with D3D11/Vulkan.
                        // We may want to revisit this with a native implementation of Metal or otherwise,
                        // but right now using Vulkan would do for the time being.
                        return GraphicsBackend.Vulkan;

                    case RuntimeInfo.Platform.Linux:
                        return GraphicsBackend.OpenGL;

                    case RuntimeInfo.Platform.Android:
                        return GraphicsBackend.OpenGLES;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(Type));
                }
            }
        }

        /// <summary>
        /// The <see cref="SDL.SDL_WindowFlags"/> required for the graphics backend to work with the window.
        /// </summary>
        public SDL.SDL_WindowFlags WindowFlags
        {
            get
            {
                switch (Type)
                {
                    case GraphicsBackend.OpenGL:
                    case GraphicsBackend.OpenGLES:
                        return SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL;

                    case GraphicsBackend.Vulkan when !RuntimeInfo.IsApple:
                        return SDL.SDL_WindowFlags.SDL_WINDOW_VULKAN;

                    case GraphicsBackend.Metal:
                    case GraphicsBackend.Vulkan when RuntimeInfo.IsApple:
                        return SDL.SDL_WindowFlags.SDL_WINDOW_METAL;

                    default:
                        return 0;
                }
            }
        }

        private IntPtr openGLContext;

        public SDL2GraphicsBackend()
        {
            // These attributes cannot be set after creating an SDL window, therefore we can only set them here.
            if (Type == GraphicsBackend.OpenGL)
            {
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 2);
            }
            else if (Type == GraphicsBackend.OpenGLES)
            {
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_ES);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 0);
            }
        }

        public void Initialise(IWindow window)
        {
            if (!(window is SDL2DesktopWindow sdl2DesktopWindow))
                throw new InvalidOperationException($"The specified window is not of type {nameof(SDL2DesktopWindow)}.");

            sdlWindow = sdl2DesktopWindow;
        }

        public Size GetDrawableSize()
        {
            int width, height;

            switch (Type)
            {
                case GraphicsBackend.OpenGL:
                case GraphicsBackend.OpenGLES:
                    SDL.SDL_GL_GetDrawableSize(sdlWindow.SDLWindowHandle, out width, out height);
                    break;

                case GraphicsBackend.Vulkan:
                    SDL.SDL_Vulkan_GetDrawableSize(sdlWindow.SDLWindowHandle, out width, out height);
                    break;

                case GraphicsBackend.Metal:
                    SDL.SDL_Metal_GetDrawableSize(sdlWindow.SDLWindowHandle, out width, out height);
                    break;

                default:
                    SDL.SDL_GetWindowSize(sdlWindow.SDLWindowHandle, out width, out height);
                    break;
            }

            return new Size(width, height);
        }

        void IHasOpenGLCapability.PrepareOpenGL(out OpenGLPlatformInfo info)
        {
            // todo: OpenGL is currently broken because a context is created here instead of at Initialize.
            // this is probably because all of these calls are happening on a different thread, since SDL is not thread-safe.
            openGLContext = SDL.SDL_GL_CreateContext(sdlWindow.SDLWindowHandle);

            if (openGLContext == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to create an SDL2 GL context ({SDL.SDL_GetError()})");

            info = new OpenGLPlatformInfo(openGLContext,
                s => SDL.SDL_GL_GetProcAddress(s),
                c => SDL.SDL_GL_MakeCurrent(sdlWindow.SDLWindowHandle, c),
                () => SDL.SDL_GL_GetCurrentContext(),
                () => SDL.SDL_GL_MakeCurrent(sdlWindow.SDLWindowHandle, IntPtr.Zero),
                c => SDL.SDL_GL_DeleteContext(c),
                () => SDL.SDL_GL_SwapWindow(sdlWindow.SDLWindowHandle),
                value => SDL.SDL_GL_SetSwapInterval(value ? 1 : 0));
        }
    }
}
