using System;
using System.Runtime.InteropServices;
using Silk.NET.SDL;

namespace TheAdventure
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_Color
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;
    }

    public static partial class Ttf
    {
        private const string Lib = "SDL2_ttf";

        [LibraryImport(Lib)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static partial int TTF_Init();

        [LibraryImport(Lib)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static partial void TTF_Quit();

        [LibraryImport(Lib)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static partial IntPtr TTF_OpenFont(
            [MarshalAs(UnmanagedType.LPStr)] string filePath,
            int pointSize);

        [LibraryImport(Lib)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static partial void TTF_CloseFont(IntPtr font);

        [LibraryImport(Lib)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static partial IntPtr TTF_RenderText_Blended(
            IntPtr font,
            [MarshalAs(UnmanagedType.LPStr)] string text,
            SDL_Color color);

        [LibraryImport(Lib)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial int TTF_SizeText(
            IntPtr font,
            [MarshalAs(UnmanagedType.LPStr)] string text,
            int* w,
            int* h);
    }
} 