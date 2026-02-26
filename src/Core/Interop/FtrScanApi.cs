using System;
using System.Runtime.InteropServices;

namespace LectorHuellas.Core.Interop
{
    /// <summary>
    /// P/Invoke for FTRAPI.dll — the high-level Futronic SDK API.
    /// Handles initialization, enrollment, verification and identification.
    /// Also keeps the low-level ftrScanAPI.dll declarations for device detection.
    /// </summary>
    public static class FtrScanApi
    {
        // ── DLL Names ───────────────────────────────────────────────────
        private const string FTRAPI_DLL = "FTRAPI.dll";
        private const string SCAN_DLL = "ftrScanAPI.dll";

        public static readonly IntPtr INVALID_HANDLE = IntPtr.Zero;

        // ── FTRAPI Return Codes ─────────────────────────────────────────
        public const int FTR_RETCODE_OK = 0;
        public const int FTR_RETCODE_NO_MEMORY = 2;
        public const int FTR_RETCODE_INVALID_ARG = 3;
        public const int FTR_RETCODE_ALREADY_IN_USE = 4;
        public const int FTR_RETCODE_INVALID_PURPOSE = 5;
        public const int FTR_RETCODE_INTERNAL_ERROR = 6;
        public const int FTR_RETCODE_UNABLE_TO_CAPTURE = 7;
        public const int FTR_RETCODE_CANCELED_BY_USER = 8;
        public const int FTR_RETCODE_NO_MORE_RETRIES = 9;
        public const int FTR_RETCODE_INCONSISTENT_SAMPLING = 11;
        public const int FTR_RETCODE_TRIAL_EXPIRED = 12;
        public const int FTR_RETCODE_FRAME_SOURCE_NOT_SET = 201;
        public const int FTR_RETCODE_DEVICE_NOT_CONNECTED = 202;
        public const int FTR_RETCODE_DEVICE_FAILURE = 203;
        public const int FTR_RETCODE_EMPTY_FRAME = 204;
        public const int FTR_RETCODE_FAKE_SOURCE = 205;
        public const int FTR_RETCODE_INCOMPATIBLE_HARDWARE = 206;
        public const int FTR_RETCODE_INCOMPATIBLE_FIRMWARE = 207;

        // ── FTRAPI Parameters ───────────────────────────────────────────
        public const uint FTR_PARAM_IMAGE_WIDTH = 1;
        public const uint FTR_PARAM_IMAGE_HEIGHT = 2;
        public const uint FTR_PARAM_IMAGE_SIZE = 3;
        public const uint FTR_PARAM_CB_FRAME_SOURCE = 4;
        public const uint FTR_PARAM_CB_CONTROL = 5;
        public const uint FTR_PARAM_MAX_TEMPLATE_SIZE = 6;
        public const uint FTR_PARAM_MAX_FAR_REQUESTED = 7;
        public const uint FTR_PARAM_SYS_ERROR_CODE = 8;
        public const uint FTR_PARAM_FAKE_DETECT = 9;
        public const uint FTR_PARAM_MAX_MODELS = 10;
        public const uint FTR_PARAM_FFD_CONTROL = 11;
        public const uint FTR_PARAM_MIOT_CONTROL = 12;
        public const uint FTR_PARAM_MAX_FARN_REQUESTED = 13;
        public const uint FTR_PARAM_VERSION = 14;
        public const uint FTR_PARAM_FAST_MODE = 16;

        // ── Frame Sources ───────────────────────────────────────────────
        public const int FSD_FUTRONIC_USB = 1;

        // ── Purposes ────────────────────────────────────────────────────
        public const uint FTR_PURPOSE_ENROLL = 3;
        public const uint FTR_PURPOSE_IDENTIFY = 2;

        // ── Callback state/signal constants ──────────────────────────────
        public const uint FTR_STATE_FRAME_PROVIDED = 0x01;
        public const uint FTR_STATE_SIGNAL_PROVIDED = 0x02;
        public const uint FTR_SIGNAL_TOUCH_SENSOR = 1;
        public const uint FTR_SIGNAL_TAKE_OFF = 2;
        public const uint FTR_SIGNAL_FAKE_SOURCE = 3;
        public const uint FTR_CONTINUE = 2;
        public const uint FTR_CANCEL = 1;

        // ── Version ─────────────────────────────────────────────────────
        public const uint FTR_VERSION_CURRENT = 3;

        // ── Structures ──────────────────────────────────────────────────
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FTR_DATA
        {
            public uint dwSize;
            public IntPtr pData;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FTR_BITMAP
        {
            public uint Width;
            public uint Height;
            public FTR_DATA Bitmap;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FTR_PROGRESS
        {
            public uint dwSize;
            public uint dwCount;
            public int bIsRepeated;
            public uint dwTotal;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FTR_ENROLL_DATA
        {
            public uint dwSize;
            public uint dwQuality;
        }

        // ── Callback delegate ───────────────────────────────────────────
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void FTR_CB_STATE_CONTROL(
            IntPtr Context,
            uint StateMask,
            ref uint pResponse,
            uint Signal,
            IntPtr pBitmap
        );

        // ── FTRAPI.dll Functions ────────────────────────────────────────
        [DllImport(FTRAPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int FTRInitialize();

        [DllImport(FTRAPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern void FTRTerminate();

        [DllImport(FTRAPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int FTRSetParam(uint Param, IntPtr Value);

        [DllImport(FTRAPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int FTRGetParam(uint Param, out IntPtr pValue);

        [DllImport(FTRAPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int FTRCaptureFrame(IntPtr UserContext, IntPtr pFrameBuf);

        [DllImport(FTRAPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int FTREnroll(IntPtr UserContext, uint Purpose, ref FTR_DATA pTemplate);

        [DllImport(FTRAPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int FTREnrollX(IntPtr UserContext, uint Purpose, ref FTR_DATA pTemplate, ref FTR_ENROLL_DATA pEData);

        [DllImport(FTRAPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int FTRVerifyN(IntPtr UserContext, ref FTR_DATA pTemplate, out int pResult, out int pFARVerify);

        [DllImport(FTRAPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int FTRSetBaseTemplate(ref FTR_DATA pTemplate);

        [DllImport(FTRAPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int FTRIdentifyN(ref FTR_IDENTIFY_ARRAY pAIdent, out uint pdwMatchCnt, ref FTR_MATCHED_X_ARRAY pAMatch);

        // ── Structures for identification ───────────────────────────────
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FTR_IDENTIFY_RECORD
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] KeyValue;
            public IntPtr pData; // Pointer to FTR_DATA
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FTR_IDENTIFY_ARRAY
        {
            public uint TotalNumber;
            public IntPtr pMembers; // Pointer to FTR_IDENTIFY_RECORD[]
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FAR_ATTAINED
        {
            public int N; // Numerical FAR value
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FTR_MATCHED_X_RECORD
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] KeyValue;
            public FAR_ATTAINED FarAttained;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FTR_MATCHED_X_ARRAY
        {
            public uint TotalNumber;
            public IntPtr pMembers; // Pointer to FTR_MATCHED_X_RECORD[]
        }

        // ── ftrScanAPI.dll Functions (for device detection only) ─────────
        [DllImport(SCAN_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ftrScanGetNumberOfDevices(out int pNumberOfDevices);

        [DllImport(SCAN_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr ftrScanOpenDevice();

        [DllImport(SCAN_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern void ftrScanCloseDevice(IntPtr ftrHandle);

        [StructLayout(LayoutKind.Sequential)]
        public struct FTRSCAN_IMAGE_SIZE
        {
            public int nWidth;
            public int nHeight;
            public int nImageSize;
        }

        [DllImport(SCAN_DLL, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ftrScanGetImageSize(IntPtr ftrHandle, out FTRSCAN_IMAGE_SIZE pImageSize);

        [DllImport(SCAN_DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int ftrScanGetLastError();

        // ── Helper ──────────────────────────────────────────────────────
        public static bool IsDllAvailable()
        {
            try
            {
                var dir = AppDomain.CurrentDomain.BaseDirectory;
                var ftrapPath = System.IO.Path.Combine(dir, FTRAPI_DLL);
                var scanPath = System.IO.Path.Combine(dir, SCAN_DLL);
                return System.IO.File.Exists(ftrapPath) && System.IO.File.Exists(scanPath);
            }
            catch { return false; }
        }

        public static string RetCodeToMessage(int code)
        {
            return code switch
            {
                FTR_RETCODE_OK => "OK",
                FTR_RETCODE_NO_MEMORY => "Sin memoria",
                FTR_RETCODE_INVALID_ARG => "Argumento inválido",
                FTR_RETCODE_ALREADY_IN_USE => "Ya en uso",
                FTR_RETCODE_INVALID_PURPOSE => "Propósito inválido",
                FTR_RETCODE_INTERNAL_ERROR => "Error interno",
                FTR_RETCODE_UNABLE_TO_CAPTURE => "No se pudo capturar",
                FTR_RETCODE_CANCELED_BY_USER => "Cancelado por usuario",
                FTR_RETCODE_NO_MORE_RETRIES => "Sin más reintentos",
                FTR_RETCODE_FRAME_SOURCE_NOT_SET => "Fuente de frame no configurada",
                FTR_RETCODE_DEVICE_NOT_CONNECTED => "Dispositivo no conectado",
                FTR_RETCODE_DEVICE_FAILURE => "Fallo del dispositivo",
                FTR_RETCODE_EMPTY_FRAME => "Frame vacío",
                FTR_RETCODE_FAKE_SOURCE => "Huella falsa detectada",
                FTR_RETCODE_INCOMPATIBLE_HARDWARE => "Hardware incompatible",
                FTR_RETCODE_INCOMPATIBLE_FIRMWARE => "Firmware incompatible",
                _ => $"Error desconocido ({code})"
            };
        }
    }
}
