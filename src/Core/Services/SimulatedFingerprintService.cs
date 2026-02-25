using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LectorHuellas.Core.Services
{
    /// <summary>
    /// Simulated fingerprint service for development without hardware.
    /// </summary>
    public class SimulatedFingerprintService : IFingerprintService
    {
        private bool _initialized;
        private readonly int _width = 320;
        private readonly int _height = 480;
        private int _captureCounter;

        public bool IsDeviceConnected => _initialized;
        public bool IsSimulated => true;
        public event Action<string>? OnStatusMessage;

        public bool Initialize()
        {
            _initialized = true;
            Console.WriteLine("Simulador: Dispositivo simulado inicializado.");
            return true;
        }

        public Task<byte[]?> CaptureImageAsync()
        {
            return Task.Run(() =>
            {
                if (!_initialized) return (byte[]?)null;
                Task.Delay(800).Wait();
                _captureCounter++;
                var imageData = GenerateSyntheticFingerprint(_captureCounter);
                return (byte[]?)imageData;
            });
        }

        public Task<(byte[]? imageData, byte[]? template)> EnrollFingerprintAsync()
        {
            return Task.Run(() =>
            {
                if (!_initialized) return ((byte[]?)null, (byte[]?)null);
                Task.Delay(1000).Wait();
                _captureCounter++;
                var imageData = GenerateSyntheticFingerprint(_captureCounter);
                var template = CreateTemplate(imageData);
                return ((byte[]?)imageData, (byte[]?)template);
            });
        }

        public Task<(int matchIndex, byte[]? imageData)> IdentifyFingerprintAsync(List<byte[]> templates)
        {
            return Task.Run(() =>
            {
                if (!_initialized || templates == null || templates.Count == 0)
                    return (-1, (byte[]?)null);
                Task.Delay(800).Wait();
                _captureCounter++;
                var imageData = GenerateSyntheticFingerprint(_captureCounter);
                // In simulation, always match the first template
                return (0, (byte[]?)imageData);
            });
        }

        public byte[] CreateTemplate(byte[] imageData)
        {
            var templateSize = imageData.Length / 10;
            var template = new byte[templateSize];
            for (int i = 0; i < templateSize; i++)
                template[i] = imageData[i * 10];
            return template;
        }

        public bool MatchTemplates(byte[] template1, byte[] template2)
        {
            if (template1 == null || template2 == null) return false;
            if (template1.Length != template2.Length) return false;
            int matchCount = 0;
            for (int i = 0; i < template1.Length; i++)
                if (Math.Abs(template1[i] - template2[i]) < 20) matchCount++;
            return (double)matchCount / template1.Length > 0.7;
        }

        public (int width, int height) GetImageSize() => (_width, _height);

        private byte[] GenerateSyntheticFingerprint(int seed)
        {
            var size = _width * _height;
            var data = new byte[size];
            var rng = new Random(seed);
            int cx = _width / 2, cy = _height / 2;
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                {
                    double dx = (x - cx) / (double)cx, dy = (y - cy) / (double)cy;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    double ridge = Math.Sin(dist * 25 + Math.Atan2(dy, dx) * 3) * 0.5 + 0.5;
                    double envelope = Math.Exp(-dist * dist * 2);
                    data[y * _width + x] = (byte)(ridge * envelope * 200 + rng.Next(0, 30));
                }
            return data;
        }

        public void Dispose() { _initialized = false; }
    }
}
