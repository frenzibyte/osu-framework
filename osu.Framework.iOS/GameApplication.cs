// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AVFoundation;
using Foundation;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using ObjCRuntime;
using osu.Framework.iOS.Bindings;
using SDL;
using SDL.iOSBindings;

namespace osu.Framework.iOS
{
    public static class GameApplication
    {
        private const string output_volume = @"outputVolume";

        private static IOSGameHost host = null!;
        private static Game game = null!;

        private static readonly OutputVolumeObserver output_volume_observer = new OutputVolumeObserver();

        public static unsafe void Main(Game target)
        {
            NativeLibrary.SetDllImportResolver(typeof(Bass).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/bass.framework/bass", assembly, path));
            NativeLibrary.SetDllImportResolver(typeof(BassFx).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/bass_fx.framework/bass_fx", assembly, path));
            NativeLibrary.SetDllImportResolver(typeof(BassMix).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/bassmix.framework/bassmix", assembly, path));
            NativeLibrary.SetDllImportResolver(typeof(SDL3).Assembly, (_, assembly, path) => NativeLibrary.Load("@rpath/SDL3.framework/SDL3", assembly, path));

            game = target;

            var sdlClass = Class.GetHandle(typeof(SDLUIKitDelegate));
            SetMethod(sdlClass.Handle, "getAppDelegateClassName", getAppDelegateClassName);

            SDL3.SDL_RunApp(0, null, &main, IntPtr.Zero);
        }

        private static NSString getAppDelegateClassName(IntPtr a, IntPtr b) => (NSString)nameof(GameAppDelegate);

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe int main(int argc, byte** argv)
        {
            var audioSession = AVAudioSession.SharedInstance();
            audioSession.AddObserver(output_volume_observer, output_volume, NSKeyValueObservingOptions.New, 0);

            host = new IOSGameHost();
            host.Run(game);

            return 0;
        }

        private class OutputVolumeObserver : NSObject
        {
            public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, nint context)
            {
                switch (keyPath)
                {
                    case output_volume:
                        AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback);
                        break;
                }
            }
        }

        [DllImport(Constants.ObjectiveCLibrary)]
        private static extern IntPtr class_replaceMethod(IntPtr classHandle, IntPtr selector, IntPtr method, string types);

        [DllImport(Constants.ObjectiveCLibrary)]
        private static extern IntPtr class_getClassMethod(IntPtr classHandle, IntPtr selector);

        [DllImport(Constants.ObjectiveCLibrary)]
        private static extern void method_setImplementation(IntPtr method1, IntPtr implementation);

        public static void SetMethod(IntPtr classHandle, string selector, Delegate action)
        {
            IntPtr targetSelector = Selector.GetHandle(selector);
            IntPtr targetMethod = class_getClassMethod(classHandle, targetSelector);
            IntPtr newMethodImplementation = Marshal.GetFunctionPointerForDelegate(action);
            method_setImplementation(targetMethod, newMethodImplementation);
        }
    }
}
