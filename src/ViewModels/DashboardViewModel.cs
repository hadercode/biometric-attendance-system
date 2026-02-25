using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LectorHuellas.Converters;
using LectorHuellas.Models;
using LectorHuellas.Services;

namespace LectorHuellas.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IFingerprintService _fingerprintService;
        private readonly AttendanceService _attendanceService;
        private readonly DispatcherTimer _clockTimer;

        [ObservableProperty]
        private string _currentTime = DateTime.Now.ToString("HH:mm:ss");

        [ObservableProperty]
        private string _currentDate = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy");

        [ObservableProperty]
        private int _totalEmployees;

        [ObservableProperty]
        private int _presentToday;

        [ObservableProperty]
        private int _absentToday;

        [ObservableProperty]
        private string _statusMessage = "Coloque el dedo en el lector para marcar asistencia";

        [ObservableProperty]
        private string _statusMessageColor = "#8B949E";

        [ObservableProperty]
        private string _identifiedEmployeeName = "";

        [ObservableProperty]
        private string _attendanceTypeBadge = "";

        [ObservableProperty]
        private bool _isCapturing;

        [ObservableProperty]
        private bool _showResult;

        [ObservableProperty]
        private BitmapSource? _fingerprintImage;

        public DashboardViewModel(IFingerprintService fingerprintService, AttendanceService attendanceService)
        {
            _fingerprintService = fingerprintService;
            _attendanceService = attendanceService;

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) =>
            {
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
                CurrentDate = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy",
                    new System.Globalization.CultureInfo("es-VE"));
            };
            _clockTimer.Start();

            _ = LoadStatsAsync();
        }

        [RelayCommand]
        private async Task RefreshStats()
        {
            await LoadStatsAsync();
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                var (total, present, absent) = await _attendanceService.GetDashboardStatsAsync();
                TotalEmployees = total;
                PresentToday = present;
                AbsentToday = absent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading stats: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CaptureAndIdentify()
        {
            if (IsCapturing) return;

            IsCapturing = true;
            ShowResult = false;
            StatusMessage = "⏳ Capturando huella... Coloque el dedo en el lector.";
            StatusMessageColor = "#FDCB6E";

            try
            {
                // Get ALL templates (multi-finger + legacy) as flat list
                var allTemplates = await _attendanceService.GetAllTemplatesForIdentificationAsync();
                if (allTemplates.Count == 0)
                {
                    StatusMessage = "⚠️ No hay empleados registrados con huellas.";
                    StatusMessageColor = "#FF7675";
                    return;
                }

                // Extract template data in order
                var templates = new System.Collections.Generic.List<byte[]>();
                foreach (var (_, templateData) in allTemplates)
                {
                    templates.Add(templateData);
                }

                // SDK identification - captures finger and matches against all templates
                var (matchIndex, imageData) = await _fingerprintService.IdentifyFingerprintAsync(templates);

                // Show fingerprint image if available
                if (imageData != null)
                {
                    var (w, h) = _fingerprintService.GetImageSize();
                    if (w > 0 && h > 0 && imageData.Length >= w * h)
                    {
                        FingerprintImage = FingerprintImageConverter.CreateBitmapFromGrayscale(imageData, w, h);
                    }
                }

                if (matchIndex < 0 || matchIndex >= allTemplates.Count)
                {
                    StatusMessage = "⚠️ Huella no reconocida. Empleado no registrado.";
                    StatusMessageColor = "#FF7675";
                    IdentifiedEmployeeName = "";
                    AttendanceTypeBadge = "";
                    ShowResult = true;
                    return;
                }

                // Map matched template index → employee ID
                var matchedEmployeeId = allTemplates[matchIndex].employeeId;
                var employee = await _attendanceService.GetEmployeeByIdAsync(matchedEmployeeId);

                if (employee == null)
                {
                    StatusMessage = "⚠️ Empleado no encontrado.";
                    StatusMessageColor = "#FF7675";
                    ShowResult = true;
                    return;
                }

                // Record attendance
                var (record, type) = await _attendanceService.RecordAttendanceAsync(employee.Id);

                IdentifiedEmployeeName = employee.FullName;
                AttendanceTypeBadge = type == AttendanceType.CheckIn ? "✅ ENTRADA" : "🚪 SALIDA";
                StatusMessage = $"Asistencia registrada: {employee.FullName}";
                StatusMessageColor = type == AttendanceType.CheckIn ? "#00B894" : "#00CEC9";
                ShowResult = true;

                await LoadStatsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                StatusMessageColor = "#FF7675";
            }
            finally
            {
                IsCapturing = false;
            }
        }
    }
}
