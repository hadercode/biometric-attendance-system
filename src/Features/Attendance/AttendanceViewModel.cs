using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LectorHuellas.Core.Models;
using LectorHuellas.Core.Services;

namespace LectorHuellas.Features.Attendance
{
    public partial class AttendanceViewModel : ObservableObject
    {
        private readonly IFingerprintService _fingerprintService;
        private readonly AttendanceService _attendanceService;

        [ObservableProperty]
        private string _statusMessage = "Coloque su dedo en el lector para marcar asistencia";

        [ObservableProperty]
        private Color _statusMessageColor = Colors.White;

        [ObservableProperty]
        private Employee? _identifiedEmployee;

        [ObservableProperty]
        private BitmapSource? _fingerprintImage;

        [ObservableProperty]
        private bool _isCapturing;

        [ObservableProperty]
        private bool _showCard;

        [ObservableProperty]
        private string _attendanceTypeBadge = string.Empty;

        [ObservableProperty]
        private ObservableCollection<AttendanceRecord> _recentRecords = new();

        public event EventHandler? AdminAccessRequested;

        public AttendanceViewModel(IFingerprintService fingerprintService, AttendanceService attendanceService)
        {
            _fingerprintService = fingerprintService;
            _attendanceService = attendanceService;
        }

        public async Task RefreshHistoryAsync()
        {
            try
            {
                var records = await _attendanceService.GetRecentRecordsAsync(10);
                RecentRecords.Clear();
                foreach (var r in records) RecentRecords.Add(r);
            }
            catch { /* Ignore */ }
        }

        [RelayCommand]
        public async Task CaptureAndIdentifyAsync()
        {
            if (IsCapturing) return;

            try
            {
                IsCapturing = true;
                ShowCard = false;
                StatusMessage = "Inicie captura y coloque el dedo...";
                StatusMessageColor = (Color)ColorConverter.ConvertFromString("#00CEC9"); // Accent

                // Unified identification and recording
                var result = await _attendanceService.IdentifyAndRecordAsync();
                
                if (result.HasValue)
                {
                    IdentifiedEmployee = result.Value.Employee;
                    AttendanceTypeBadge = result.Value.Type == AttendanceType.CheckIn ? "ENTRADA REGISTRADA" : "SALIDA REGISTRADA";
                    StatusMessage = "¡Bienvenido!";
                    StatusMessageColor = (Color)ColorConverter.ConvertFromString("#00B894"); // Success
                    ShowCard = true;

                    await RefreshHistoryAsync();

                    // Auto-hide card after 5 seconds
                    _ = Task.Delay(5000).ContinueWith(_ => { 
                        ShowCard = false; 
                        IdentifiedEmployee = null; 
                        StatusMessage = "Coloque su dedo en el lector para marcar asistencia";
                        StatusMessageColor = Colors.White;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    StatusMessage = "Huella no reconocida. Intente de nuevo.";
                    StatusMessageColor = (Color)ColorConverter.ConvertFromString("#FF7675"); // Danger
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageColor = (Color)ColorConverter.ConvertFromString("#FF7675");
            }
            finally
            {
                IsCapturing = false;
            }
        }

        [RelayCommand]
        private void RequestAdminAccess()
        {
            AdminAccessRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
