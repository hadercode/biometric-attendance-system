using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LectorHuellas.Core.Services
{
    public interface IFingerprintService : IDisposable
    {
        /// <summary>
        /// Indicates whether the service is connected to a real device
        /// </summary>
        bool IsDeviceConnected { get; }

        /// <summary>
        /// Whether this is a simulated service (no real hardware)
        /// </summary>
        bool IsSimulated { get; }

        /// <summary>
        /// Event fired during enrollment/identification with status messages
        /// (e.g. "place finger", "remove finger")
        /// </summary>
        event Action<string>? OnStatusMessage;

        /// <summary>
        /// Initialize the fingerprint device
        /// </summary>
        bool Initialize();

        /// <summary>
        /// Capture a fingerprint image from the scanner (raw grayscale for display).
        /// Returns raw image bytes or null if capture failed.
        /// </summary>
        Task<byte[]?> CaptureImageAsync();

        /// <summary>
        /// Enroll a fingerprint: captures the finger, creates a proper SDK template,
        /// and returns both the display image and the template.
        /// </summary>
        Task<(byte[]? imageData, byte[]? template)> EnrollFingerprintAsync();

        /// <summary>
        /// Identify a captured fingerprint against a list of stored templates.
        /// Captures the finger and compares against all provided templates.
        /// Returns the index of the matched template, or -1 if no match.
        /// Also returns the display image captured during identification.
        /// </summary>
        Task<(int matchIndex, byte[]? imageData)> IdentifyFingerprintAsync(List<byte[]> templates);

        /// <summary>
        /// Create a fingerprint template from raw image data (legacy/fallback)
        /// </summary>
        byte[] CreateTemplate(byte[] imageData);

        /// <summary>
        /// Compare two fingerprint templates and return whether they match (legacy/fallback)
        /// </summary>
        bool MatchTemplates(byte[] template1, byte[] template2);

        /// <summary>
        /// Get the image dimensions from the scanner
        /// </summary>
        (int width, int height) GetImageSize();

        /// <summary>
        /// Explicitly cancel any pending SDK operation (enrollment/identification)
        /// </summary>
        void CancelCurrentOperation();
    }
}
