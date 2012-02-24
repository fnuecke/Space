using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Awesomium.Core;
using Microsoft.Xna.Framework.Graphics;

namespace Awesomium.Xna
{
    #region Direct3D9

    internal enum D3DFORMAT
    {
        D3DFMT_UNKNOWN = 0,

        D3DFMT_R8G8B8 = 20,
        D3DFMT_A8R8G8B8 = 21,
        D3DFMT_X8R8G8B8 = 22,
        D3DFMT_R5G6B5 = 23,
        D3DFMT_X1R5G5B5 = 24,
        D3DFMT_A1R5G5B5 = 25,
        D3DFMT_A4R4G4B4 = 26,
        D3DFMT_R3G3B2 = 27,
        D3DFMT_A8 = 28,
        D3DFMT_A8R3G3B2 = 29,
        D3DFMT_X4R4G4B4 = 30,
        D3DFMT_A2B10G10R10 = 31,
        D3DFMT_A8B8G8R8 = 32,
        D3DFMT_X8B8G8R8 = 33,
        D3DFMT_G16R16 = 34,
        D3DFMT_A2R10G10B10 = 35,
        D3DFMT_A16B16G16R16 = 36,

        D3DFMT_A8P8 = 40,
        D3DFMT_P8 = 41,

        D3DFMT_L8 = 50,
        D3DFMT_A8L8 = 51,
        D3DFMT_A4L4 = 52,

        D3DFMT_V8U8 = 60,
        D3DFMT_L6V5U5 = 61,
        D3DFMT_X8L8V8U8 = 62,
        D3DFMT_Q8W8V8U8 = 63,
        D3DFMT_V16U16 = 64,
        D3DFMT_A2W10V10U10 = 67,

        //TODO Check MAKEFOURCC conversions
        D3DFMT_UYVY = ('U' << 24) + ('Y' << 16) + ('V' << 8) + 'Y', //MAKEFOURCC('U', 'Y', 'V', 'Y'),
        D3DFMT_R8G8_B8G8 = ('R' << 24) + ('G' << 16) + ('B' << 8) + 'G', //MAKEFOURCC('R', 'G', 'B', 'G'),
        D3DFMT_YUY2 = ('Y' << 24) + ('U' << 16) + ('Y' << 8) + '2', //MAKEFOURCC('Y', 'U', 'Y', '2'),
        D3DFMT_G8R8_G8B8 = ('G' << 24) + ('R' << 16) + ('G' << 8) + 'B', //MAKEFOURCC('G', 'R', 'G', 'B'),
        D3DFMT_DXT1 = ('D' << 24) + ('X' << 16) + ('T' << 8) + '1', //MAKEFOURCC('D', 'X', 'T', '1'),
        D3DFMT_DXT2 = ('D' << 24) + ('X' << 16) + ('T' << 8) + '2', //MAKEFOURCC('D', 'X', 'T', '2'),
        D3DFMT_DXT3 = ('D' << 24) + ('X' << 16) + ('T' << 8) + '3', //MAKEFOURCC('D', 'X', 'T', '3'),
        D3DFMT_DXT4 = ('D' << 24) + ('X' << 16) + ('T' << 8) + '4', //MAKEFOURCC('D', 'X', 'T', '4'),
        D3DFMT_DXT5 = ('D' << 24) + ('X' << 16) + ('T' << 8) + '5', //MAKEFOURCC('D', 'X', 'T', '5'),

        D3DFMT_D16_LOCKABLE = 70,
        D3DFMT_D32 = 71,
        D3DFMT_D15S1 = 73,
        D3DFMT_D24S8 = 75,
        D3DFMT_D24X8 = 77,
        D3DFMT_D24X4S4 = 79,
        D3DFMT_D16 = 80,

        D3DFMT_D32F_LOCKABLE = 82,
        D3DFMT_D24FS8 = 83,

        D3DFMT_D32_LOCKABLE = 84,
        D3DFMT_S8_LOCKABLE = 85,

        D3DFMT_L16 = 81,

        D3DFMT_VERTEXDATA = 100,
        D3DFMT_INDEX16 = 101,
        D3DFMT_INDEX32 = 102,

        D3DFMT_Q16W16V16U16 = 110,

        D3DFMT_MULTI2_ARGB8 = ('M' << 24) + ('E' << 16) + ('T' << 8) + '1', //MAKEFOURCC('M','E','T','1'),

        D3DFMT_R16F = 111,
        D3DFMT_G16R16F = 112,
        D3DFMT_A16B16G16R16F = 113,

        D3DFMT_R32F = 114,
        D3DFMT_G32R32F = 115,
        D3DFMT_A32B32G32R32F = 116,

        D3DFMT_CxV8U8 = 117,

        D3DFMT_A1 = 118,
        D3DFMT_A2B10G10R10_XR_BIAS = 119,
        D3DFMT_BINARYBUFFER = 199,

        D3DFMT_FORCE_DWORD = 0x7fffffff
    }

    internal enum D3DRESOURCETYPE
    {
        D3DRTYPE_SURFACE = 1,
        D3DRTYPE_VOLUME = 2,
        D3DRTYPE_TEXTURE = 3,
        D3DRTYPE_VOLUMETEXTURE = 4,
        D3DRTYPE_CubeTexture = 5,
        D3DRTYPE_VERTEXBUFFER = 6,
        D3DRTYPE_INDEXBUFFER = 7,
        D3DRTYPE_FORCE_DWORD = 0x7fffffff
    }

    internal enum D3DPOOL
    {
        D3DPOOL_DEFAULT = 0,
        D3DPOOL_MANAGED = 1,
        D3DPOOL_SYSTEMMEM = 2,
        D3DPOOL_SCRATCH = 3,
        D3DPOOL_FORCE_DWORD = 0x7fffffff
    }

    internal enum D3DMULTISAMPLE_TYPE
    {
        D3DMULTISAMPLE_NONE = 0,
        D3DMULTISAMPLE_NONMASKABLE = 1,
        D3DMULTISAMPLE_2_SAMPLES = 2,
        D3DMULTISAMPLE_3_SAMPLES = 3,
        D3DMULTISAMPLE_4_SAMPLES = 4,
        D3DMULTISAMPLE_5_SAMPLES = 5,
        D3DMULTISAMPLE_6_SAMPLES = 6,
        D3DMULTISAMPLE_7_SAMPLES = 7,
        D3DMULTISAMPLE_8_SAMPLES = 8,
        D3DMULTISAMPLE_9_SAMPLES = 9,
        D3DMULTISAMPLE_10_SAMPLES = 10,
        D3DMULTISAMPLE_11_SAMPLES = 11,
        D3DMULTISAMPLE_12_SAMPLES = 12,
        D3DMULTISAMPLE_13_SAMPLES = 13,
        D3DMULTISAMPLE_14_SAMPLES = 14,
        D3DMULTISAMPLE_15_SAMPLES = 15,
        D3DMULTISAMPLE_16_SAMPLES = 16,
        D3DMULTISAMPLE_FORCE_DWORD = -1,
    }

    internal struct D3DSURFACE_DESC
    {
        public D3DFORMAT Format;
        public D3DRESOURCETYPE Type;
        public int Usage;
        public D3DPOOL Pool;
        public D3DMULTISAMPLE_TYPE MultiSampleType;
        public int MultiSampleQuality;
        public uint Width;
        public uint Height;
    }

    internal unsafe struct D3DLOCKED_RECT
    {
        public int Pitch;
        public void* pBits;
    }

    internal struct RECT
    {
        int x1;
        int y1;
        int x2;
        int y2;
    }

    [ComImport, Guid("85C31227-3DE5-4f00-9B3A-F11AC38C18B5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDirect3DTexture9
    {
        void GetDevice();
        void SetPrivateData();
        void GetPrivateData();
        void FreePrivateData();
        void SetPriority();
        void GetPriority();
        void PreLoad();
        void GetType();
        void SetLOD();
        void GetLOD();
        void GetLevelCount();
        void SetAutoGenFilterType();
        void GetAutoGenFilterType();
        void GenerateMipSubLevels();
        int GetLevelDesc(uint level, out D3DSURFACE_DESC Desc);
        int GetSurfaceLevel(uint level, out IntPtr surfacePointer);
        int LockRect(uint level, out D3DLOCKED_RECT LockedRect, RECT rect, int Flags);
        int UnlockRect(uint level);
        int AddDirtyRect(RECT pDirtyRect);
    }

    #endregion

    internal static class TextureFormatConverter
    {
        #region XNA To DirectX by Reflection

        public static unsafe void DirectBlit(RenderBuffer buffer, ref Texture2D texture2D)
        {
            var d3Dt = GetIUnknownObject<IDirect3DTexture9>(texture2D);

            D3DLOCKED_RECT lockRect;
            var rect = new RECT();
            Marshal.ThrowExceptionForHR(d3Dt.LockRect(0, out  lockRect, rect, 0));

            buffer.CopyTo((IntPtr)(uint)(lockRect.pBits), lockRect.Pitch, 4, false);
            d3Dt.UnlockRect(0);

            // Move onto Dispose() if d3Dt will be cached d3Dt
            Marshal.ReleaseComObject(d3Dt);
        }

        private static T GetIUnknownObject<T>(object container)
        {
            unsafe
            {
                // Get the COM object pointer from the D3D object and marshal it as one of the interfaces defined below
                var dField = container.GetType().GetField("pComPtr", BindingFlags.NonPublic | BindingFlags.Instance);
                var dPointer = new IntPtr(Pointer.Unbox(dField.GetValue(container)));
                return (T)Marshal.GetObjectForIUnknown(dPointer);
            }
        }

        #endregion
    }
}