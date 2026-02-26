using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LectorHuellas.Core.Services
{
    /// <summary>
    /// Proxy service that allows swapping the underlying fingerprint implementation at runtime.
    /// This is used to switch from Simulated to Real (Futronic) when a user clicks "Retry".
    /// </summary>
    public class ScannerProxyService : IFingerprintService
    {
        private IFingerprintService _internalService;

        public ScannerProxyService(IFingerprintService initialService)
        {
            _internalService = initialService;
            _internalService.OnStatusMessage += (msg) => OnStatusMessage?.Invoke(msg);
        }

        public bool IsDeviceConnected => _internalService.IsDeviceConnected;
        public bool IsSimulated => _internalService.IsSimulated;
        public event Action<string>? OnStatusMessage;

        public void SetInternalService(IFingerprintService newService)
        {
            // Clean up old service events
            // Note: We don't necessarily dispose it here as MainViewModel might handle that,
            // but for safety, we swap.
            _internalService.OnStatusMessage -= (msg) => OnStatusMessage?.Invoke(msg);
            
            _internalService = newService;
            _internalService.OnStatusMessage += (msg) => OnStatusMessage?.Invoke(msg);
        }

        public bool Initialize() => _internalService.Initialize();

        public Task<byte[]?> CaptureImageAsync() => _internalService.CaptureImageAsync();

        public Task<(byte[]? imageData, byte[]? template)> EnrollFingerprintAsync() => _internalService.EnrollFingerprintAsync();

        public Task<(int matchIndex, byte[]? imageData)> IdentifyFingerprintAsync(List<byte[]> templates) => _internalService.IdentifyFingerprintAsync(templates);

        public byte[] CreateTemplate(byte[] imageData) => _internalService.CreateTemplate(imageData);

        public bool MatchTemplates(byte[] template1, byte[] template2) => _internalService.MatchTemplates(template1, template2);

        public (int width, int height) GetImageSize() => _internalService.GetImageSize();

        public bool CheckDevicePresence() => _internalService.CheckDevicePresence();

        public void CancelCurrentOperation() => _internalService.CancelCurrentOperation();

        public void Dispose()
        {
            _internalService?.Dispose();
        }
    }
}
