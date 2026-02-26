using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LectorHuellas.Core.Interop;

namespace LectorHuellas.Core.Services
{
    /// <summary>
    /// Real implementation using the Futronic FS80H scanner via FTRAPI.dll.
    /// Uses FTREnrollX for enrollment and FTRSetBaseTemplate + FTRIdentifyN for identification.
    /// </summary>
    public class FutronicService : IFingerprintService, IDisposable
    {
        private bool _initialized;
        private bool _disposed;
        private int _imageWidth;
        private int _imageHeight;
        private int _imageSize;
        private int _maxTemplateSize;
        private bool _isOperationCancelled;

        // Keep callback alive to prevent GC
        private FtrScanApi.FTR_CB_STATE_CONTROL? _callbackDelegate;

        // Captured image from callback
        private byte[]? _lastCapturedImage;

        public bool IsDeviceConnected => _initialized;
        public bool IsSimulated => false;
        public event Action<string>? OnStatusMessage;

        public bool Initialize()
        {
            try
            {
                int result = FtrScanApi.FTRInitialize();
                if (result != FtrScanApi.FTR_RETCODE_OK &&
                    result != FtrScanApi.FTR_RETCODE_ALREADY_IN_USE)
                {
                    Console.WriteLine($"FTRAPI: FTRInitialize falló. Error: {result} ({FtrScanApi.RetCodeToMessage(result)})");
                    return false;
                }
                Console.WriteLine("FTRAPI: Inicializado correctamente.");

                // Set frame source to USB device
                result = FtrScanApi.FTRSetParam(FtrScanApi.FTR_PARAM_CB_FRAME_SOURCE, (IntPtr)FtrScanApi.FSD_FUTRONIC_USB);
                if (result != FtrScanApi.FTR_RETCODE_OK)
                {
                    Console.WriteLine($"FTRAPI: FTRSetParam(FRAME_SOURCE) falló. Error: {result}");
                    FtrScanApi.FTRTerminate();
                    return false;
                }

                // Set callback
                _callbackDelegate = new FtrScanApi.FTR_CB_STATE_CONTROL(StateControlCallback);
                var callbackPtr = Marshal.GetFunctionPointerForDelegate(_callbackDelegate);
                result = FtrScanApi.FTRSetParam(FtrScanApi.FTR_PARAM_CB_CONTROL, callbackPtr);
                if (result != FtrScanApi.FTR_RETCODE_OK)
                {
                    Console.WriteLine($"FTRAPI: FTRSetParam(CB_CONTROL) falló. Error: {result}");
                    FtrScanApi.FTRTerminate();
                    return false;
                }

                // Disable fake detection
                FtrScanApi.FTRSetParam(FtrScanApi.FTR_PARAM_FAKE_DETECT, IntPtr.Zero);

                // Set version
                FtrScanApi.FTRSetParam(FtrScanApi.FTR_PARAM_VERSION, (IntPtr)FtrScanApi.FTR_VERSION_CURRENT);

                // Set FARN level (normal = 166)
                FtrScanApi.FTRSetParam(FtrScanApi.FTR_PARAM_MAX_FARN_REQUESTED, (IntPtr)166);

                // Get image parameters
                IntPtr val;
                FtrScanApi.FTRGetParam(FtrScanApi.FTR_PARAM_IMAGE_WIDTH, out val);
                _imageWidth = val.ToInt32();
                FtrScanApi.FTRGetParam(FtrScanApi.FTR_PARAM_IMAGE_HEIGHT, out val);
                _imageHeight = val.ToInt32();
                FtrScanApi.FTRGetParam(FtrScanApi.FTR_PARAM_IMAGE_SIZE, out val);
                _imageSize = val.ToInt32();
                FtrScanApi.FTRGetParam(FtrScanApi.FTR_PARAM_MAX_TEMPLATE_SIZE, out val);
                _maxTemplateSize = val.ToInt32();

                Console.WriteLine($"FTRAPI: Imagen: {_imageWidth}x{_imageHeight}, Size: {_imageSize}, MaxTemplate: {_maxTemplateSize}");
                _initialized = true;
                return true;
            }
            catch (DllNotFoundException ex)
            {
                Console.WriteLine($"FTRAPI: DLL no encontrada - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FTRAPI: Error al inicializar - {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Callback from FTRAPI during capture/enrollment operations.
        /// </summary>
        private void StateControlCallback(IntPtr Context, uint StateMask, ref uint pResponse, uint Signal, IntPtr pBitmap)
        {
            pResponse = _isOperationCancelled ? FtrScanApi.FTR_CANCEL : FtrScanApi.FTR_CONTINUE;

            if ((StateMask & FtrScanApi.FTR_STATE_SIGNAL_PROVIDED) != 0)
            {
                switch (Signal)
                {
                    case FtrScanApi.FTR_SIGNAL_TOUCH_SENSOR:
                        Console.WriteLine("FTRAPI: 👆 Coloque el dedo en el lector...");
                        OnStatusMessage?.Invoke("👆 Coloque el dedo en el lector...");
                        break;
                    case FtrScanApi.FTR_SIGNAL_TAKE_OFF:
                        Console.WriteLine("FTRAPI: ✋ Retire el dedo del lector...");
                        OnStatusMessage?.Invoke("✋ Retire el dedo del lector...");
                        break;
                    case FtrScanApi.FTR_SIGNAL_FAKE_SOURCE:
                        Console.WriteLine("FTRAPI: ⚠️ Huella falsa detectada.");
                        break;
                }
            }

            if ((StateMask & FtrScanApi.FTR_STATE_FRAME_PROVIDED) != 0 && pBitmap != IntPtr.Zero)
            {
                try
                {
                    var bitmap = Marshal.PtrToStructure<FtrScanApi.FTR_BITMAP>(pBitmap);
                    if (bitmap.Bitmap.pData != IntPtr.Zero && bitmap.Bitmap.dwSize > 0)
                    {
                        _lastCapturedImage = new byte[bitmap.Bitmap.dwSize];
                        Marshal.Copy(bitmap.Bitmap.pData, _lastCapturedImage, 0, (int)bitmap.Bitmap.dwSize);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FTRAPI: Error leyendo bitmap: {ex.Message}");
                }
            }
        }

        // ── Simple frame capture (for display only) ─────────────────────
        public Task<byte[]?> CaptureImageAsync()
        {
            return Task.Run(() =>
            {
                if (!_initialized) return null;

                try
                {
                    IntPtr frameBuffer = Marshal.AllocHGlobal(_imageSize);
                    try
                    {
                        Console.WriteLine("FTRAPI: Capturando frame...");
                        int result = FtrScanApi.FTRCaptureFrame(IntPtr.Zero, frameBuffer);
                        if (result == FtrScanApi.FTR_RETCODE_OK)
                        {
                            byte[] imageData = new byte[_imageSize];
                            Marshal.Copy(frameBuffer, imageData, 0, _imageSize);
                            Console.WriteLine($"FTRAPI: ✅ Frame capturado ({_imageSize} bytes).");
                            return imageData;
                        }
                        Console.WriteLine($"FTRAPI: ❌ FTRCaptureFrame falló. Error: {result} ({FtrScanApi.RetCodeToMessage(result)})");
                        return _lastCapturedImage;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(frameBuffer);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FTRAPI: ❌ Excepción: {ex.Message}");
                    return null;
                }
            });
        }

        // ── SDK Enrollment: capture + create proper template ────────────
        public Task<(byte[]? imageData, byte[]? template)> EnrollFingerprintAsync()
        {
            return Task.Run(() =>
            {
                if (!_initialized)
                {
                    Console.WriteLine("FTRAPI: No inicializado.");
                    return ((byte[]?)null, (byte[]?)null);
                }

                try
                {
                    _lastCapturedImage = null;

                    IntPtr templateBuffer = Marshal.AllocHGlobal(_maxTemplateSize);
                    try
                    {
                        var templateData = new FtrScanApi.FTR_DATA
                        {
                            dwSize = (uint)_maxTemplateSize,
                            pData = templateBuffer
                        };

                        var enrollData = new FtrScanApi.FTR_ENROLL_DATA
                        {
                            dwSize = (uint)Marshal.SizeOf<FtrScanApi.FTR_ENROLL_DATA>()
                        };

                        Console.WriteLine("FTRAPI: 📝 Enrollment - Coloque el dedo en el lector...");
                        _isOperationCancelled = false;
                        int result = FtrScanApi.FTREnrollX(IntPtr.Zero, FtrScanApi.FTR_PURPOSE_ENROLL, ref templateData, ref enrollData);

                        if (result == FtrScanApi.FTR_RETCODE_OK)
                        {
                            byte[] template = new byte[templateData.dwSize];
                            Marshal.Copy(templateData.pData, template, 0, (int)templateData.dwSize);
                            Console.WriteLine($"FTRAPI: ✅ Enrollment exitoso. Template: {template.Length} bytes, Calidad: {enrollData.dwQuality}");

                            // Use the last captured image from callback for display
                            byte[]? imageData = _lastCapturedImage;
                            if (imageData == null)
                            {
                                // Fallback: generate a simple preview
                                Console.WriteLine("FTRAPI: ℹ️ Sin preview del callback. Usando frame vacío.");
                                imageData = new byte[_imageSize];
                            }

                            return (imageData, (byte[]?)template);
                        }
                        else
                        {
                            Console.WriteLine($"FTRAPI: ❌ FTREnrollX falló. Error: {result} ({FtrScanApi.RetCodeToMessage(result)})");
                            return ((byte[]?)null, (byte[]?)null);
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(templateBuffer);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FTRAPI: ❌ Excepción en enrollment: {ex.Message}");
                    return ((byte[]?)null, (byte[]?)null);
                }
            });
        }

        // ── SDK Identification: capture + match against multiple templates ─
        public Task<(int matchIndex, byte[]? imageData)> IdentifyFingerprintAsync(List<byte[]> templates)
        {
            return Task.Run(() =>
            {
                if (!_initialized || templates == null || templates.Count == 0)
                    return (-1, (byte[]?)null);

                try
                {
                    _lastCapturedImage = null;

                    // Step 1: Enroll a base template for identification
                    IntPtr baseBuffer = Marshal.AllocHGlobal(_maxTemplateSize);
                    try
                    {
                        var baseData = new FtrScanApi.FTR_DATA
                        {
                            dwSize = (uint)_maxTemplateSize,
                            pData = baseBuffer
                        };

                        Console.WriteLine("FTRAPI: 🔍 Identificación - Coloque el dedo en el lector...");
                        var enrollData = new FtrScanApi.FTR_ENROLL_DATA
                        {
                            dwSize = (uint)Marshal.SizeOf<FtrScanApi.FTR_ENROLL_DATA>()
                        };
                        _isOperationCancelled = false;
                        int result = FtrScanApi.FTREnrollX(IntPtr.Zero, FtrScanApi.FTR_PURPOSE_IDENTIFY, ref baseData, ref enrollData);

                        if (result != FtrScanApi.FTR_RETCODE_OK)
                        {
                            Console.WriteLine($"FTRAPI: ❌ FTREnrollX(IDENTIFY) falló. Error: {result} ({FtrScanApi.RetCodeToMessage(result)})");
                            int matchIndex = (result == FtrScanApi.FTR_RETCODE_CANCELED_BY_USER || _isOperationCancelled) ? -2 : -1;
                            return (matchIndex, _lastCapturedImage);
                        }

                        Console.WriteLine("FTRAPI: Base template creado. Comparando...");

                        // Step 2: Set base template
                        result = FtrScanApi.FTRSetBaseTemplate(ref baseData);
                        if (result != FtrScanApi.FTR_RETCODE_OK)
                        {
                            Console.WriteLine($"FTRAPI: ❌ FTRSetBaseTemplate falló. Error: {result}");
                            return (-1, _lastCapturedImage);
                        }

                        // Step 3: Build identification array
                        int count = templates.Count;
                        int recordSize = Marshal.SizeOf<FtrScanApi.FTR_IDENTIFY_RECORD>();
                        int dataSize = Marshal.SizeOf<FtrScanApi.FTR_DATA>();

                        IntPtr recordsPtr = Marshal.AllocHGlobal(recordSize * count);
                        var dataBuffers = new IntPtr[count];
                        var dataPtrs = new IntPtr[count];

                        try
                        {
                            for (int i = 0; i < count; i++)
                            {
                                // Allocate FTR_DATA for this template
                                dataBuffers[i] = Marshal.AllocHGlobal(templates[i].Length);
                                Marshal.Copy(templates[i], 0, dataBuffers[i], templates[i].Length);

                                dataPtrs[i] = Marshal.AllocHGlobal(dataSize);
                                var ftrData = new FtrScanApi.FTR_DATA
                                {
                                    dwSize = (uint)templates[i].Length,
                                    pData = dataBuffers[i]
                                };
                                Marshal.StructureToPtr(ftrData, dataPtrs[i], false);

                                // Create identify record
                                var record = new FtrScanApi.FTR_IDENTIFY_RECORD
                                {
                                    KeyValue = new byte[16],
                                    pData = dataPtrs[i]
                                };
                                // Set key = index
                                BitConverter.GetBytes(i).CopyTo(record.KeyValue, 0);

                                IntPtr recordTarget = recordsPtr + (recordSize * i);
                                Marshal.StructureToPtr(record, recordTarget, false);
                            }

                            var identArray = new FtrScanApi.FTR_IDENTIFY_ARRAY
                            {
                                TotalNumber = (uint)count,
                                pMembers = recordsPtr
                            };

                            // Allocate match results
                            int matchRecordSize = Marshal.SizeOf<FtrScanApi.FTR_MATCHED_X_RECORD>();
                            IntPtr matchPtr = Marshal.AllocHGlobal(matchRecordSize * count);
                            try
                            {
                                // Initialize match array
                                for (int i = 0; i < count; i++)
                                {
                                    var emptyMatch = new FtrScanApi.FTR_MATCHED_X_RECORD
                                    {
                                        KeyValue = new byte[16],
                                        FarAttained = new FtrScanApi.FAR_ATTAINED()
                                    };
                                    Marshal.StructureToPtr(emptyMatch, matchPtr + (matchRecordSize * i), false);
                                }

                                var matchArray = new FtrScanApi.FTR_MATCHED_X_ARRAY
                                {
                                    TotalNumber = (uint)count,
                                    pMembers = matchPtr
                                };

                                // Step 4: Identify!
                                uint matchCount = 0;
                                result = FtrScanApi.FTRIdentifyN(ref identArray, out matchCount, ref matchArray);

                                if (result == FtrScanApi.FTR_RETCODE_OK && matchCount > 0)
                                {
                                    // Read first match
                                    var matched = Marshal.PtrToStructure<FtrScanApi.FTR_MATCHED_X_RECORD>(matchPtr);
                                    int matchedIndex = BitConverter.ToInt32(matched.KeyValue, 0);
                                    Console.WriteLine($"FTRAPI: ✅ Identificado! Índice: {matchedIndex}, FARN: {matched.FarAttained.N}, Matches: {matchCount}");
                                    return (matchedIndex, _lastCapturedImage);
                                }
                                else
                                {
                                    Console.WriteLine($"FTRAPI: ⚠️ No se encontró coincidencia. Result: {result}, Matches: {matchCount}");
                                    return (-1, _lastCapturedImage);
                                }
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(matchPtr);
                            }
                        }
                        finally
                        {
                            for (int i = 0; i < count; i++)
                            {
                                if (dataBuffers[i] != IntPtr.Zero) Marshal.FreeHGlobal(dataBuffers[i]);
                                if (dataPtrs[i] != IntPtr.Zero) Marshal.FreeHGlobal(dataPtrs[i]);
                            }
                            Marshal.FreeHGlobal(recordsPtr);
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(baseBuffer);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FTRAPI: ❌ Excepción en identificación: {ex.Message}");
                    Console.WriteLine($"         {ex.StackTrace}");
                    return (-1, _lastCapturedImage);
                }
            });
        }

        // ── Legacy methods (still needed by interface) ──────────────────
        public byte[] CreateTemplate(byte[] imageData) => imageData;

        public bool MatchTemplates(byte[] template1, byte[] template2)
        {
            // Not used when using SDK identification
            return false;
        }

        public (int width, int height) GetImageSize() => (_imageWidth, _imageHeight);

        public static bool StaticCheckPresence()
        {
            try
            {
                // Most direct way: check how many devices are recognized by the low-level driver
                int count = 0;
                if (FtrScanApi.ftrScanGetNumberOfDevices(out count))
                {
                    return count > 0;
                }
                
                // Fallback: try to open one directly
                IntPtr handle = FtrScanApi.ftrScanOpenDevice();
                if (handle != IntPtr.Zero)
                {
                    FtrScanApi.ftrScanCloseDevice(handle);
                    return true;
                }
                
                return false;
            }
            catch { return false; }
        }

        public bool CheckDevicePresence()
        {
            bool present = StaticCheckPresence();
            if (!present) _initialized = false;
            return present;
        }

        public void CancelCurrentOperation()
        {
            _isOperationCancelled = true;
            Console.WriteLine("FTRAPI: Solicitud de cancelación enviada.");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_initialized)
                {
                    try { FtrScanApi.FTRTerminate(); } catch { }
                    Console.WriteLine("FTRAPI: Terminado.");
                }
                _disposed = true;
                _initialized = false;
            }
        }
    }
}
