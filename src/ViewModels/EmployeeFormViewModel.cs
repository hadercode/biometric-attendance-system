using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    public partial class EmployeeFormViewModel : ObservableObject
    {
        private readonly IFingerprintService _fingerprintService;
        private readonly AttendanceService _attendanceService;
        private readonly Dispatcher _dispatcher;

        private int? _editingEmployeeId;

        // Multi-finger enrollment storage
        private readonly Dictionary<FingerType, byte[]> _enrolledFingers = new();

        [ObservableProperty]
        private string _fullName = "";

        [ObservableProperty]
        private string _documentId = "";

        [ObservableProperty]
        private string _formTitle = "Nuevo Empleado";

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _hasFingerprint;

        [ObservableProperty]
        private bool _isCapturing;

        [ObservableProperty]
        private string _captureStatus = "Seleccione un dedo y presione Capturar";

        [ObservableProperty]
        private string _captureStatusColor = "#8B949E";

        [ObservableProperty]
        private BitmapSource? _fingerprintImage;

        [ObservableProperty]
        private string _validationMessage = "";

        [ObservableProperty]
        private FingerType? _selectedFinger;

        [ObservableProperty]
        private ObservableCollection<FingerType> _enrolledFingersList = new();

        [ObservableProperty]
        private int _enrolledCount;

        public event EventHandler? SaveCompleted;

        public EmployeeFormViewModel(IFingerprintService fingerprintService, AttendanceService attendanceService)
        {
            _fingerprintService = fingerprintService;
            _attendanceService = attendanceService;
            _dispatcher = Dispatcher.CurrentDispatcher;

            // Subscribe to real-time status messages from the SDK
            _fingerprintService.OnStatusMessage += msg =>
            {
                _dispatcher.Invoke(() =>
                {
                    CaptureStatus = msg;
                    CaptureStatusColor = "#FDCB6E";
                });
            };
        }

        public void Clear()
        {
            _editingEmployeeId = null;
            FullName = "";
            DocumentId = "";
            FormTitle = "Nuevo Empleado";
            IsEditing = false;
            HasFingerprint = false;
            _enrolledFingers.Clear();
            EnrolledFingersList = new ObservableCollection<FingerType>();
            EnrolledCount = 0;
            SelectedFinger = null;
            CaptureStatus = "Seleccione un dedo y presione Capturar";
            CaptureStatusColor = "#8B949E";
            FingerprintImage = null;
            ValidationMessage = "";
        }

        public async void LoadEmployee(Employee employee)
        {
            _editingEmployeeId = employee.Id;
            FullName = employee.FullName;
            DocumentId = employee.DocumentId;
            FormTitle = "Editar Empleado";
            IsEditing = true;
            FingerprintImage = null;
            ValidationMessage = "";

            // Load existing fingerprints
            _enrolledFingers.Clear();
            var fingerprints = await _attendanceService.GetEmployeeFingerprintsAsync(employee.Id);
            foreach (var fp in fingerprints)
            {
                _enrolledFingers[fp.FingerType] = fp.TemplateData;
            }

            // Also check legacy single template
            if (_enrolledFingers.Count == 0 && employee.FingerprintTemplate != null)
            {
                _enrolledFingers[FingerType.RightIndex] = employee.FingerprintTemplate;
            }

            RefreshEnrolledUI();
            SelectedFinger = null;

            if (_enrolledFingers.Count > 0)
            {
                CaptureStatus = $"✅ {_enrolledFingers.Count} huella(s) registrada(s)";
                CaptureStatusColor = "#00B894";
            }
            else
            {
                CaptureStatus = "Seleccione un dedo y presione Capturar";
                CaptureStatusColor = "#8B949E";
            }
        }

        private void RefreshEnrolledUI()
        {
            EnrolledFingersList = new ObservableCollection<FingerType>(_enrolledFingers.Keys.OrderBy(f => (int)f));
            EnrolledCount = _enrolledFingers.Count;
            HasFingerprint = _enrolledFingers.Count > 0;
        }

        [RelayCommand]
        private async Task CaptureFingerprint()
        {
            if (IsCapturing) return;

            if (!SelectedFinger.HasValue)
            {
                ValidationMessage = "Debe seleccionar un dedo primero.";
                return;
            }

            IsCapturing = true;
            var finger = SelectedFinger.Value;
            CaptureStatus = $"⏳ Enrollment {finger.ToDisplayName()} — Coloque el dedo en el lector...";
            CaptureStatusColor = "#FDCB6E";
            ValidationMessage = "";

            try
            {
                Console.WriteLine($"UI: Enrollment de {finger.ToDisplayName()}...");
                var (imageData, template) = await _fingerprintService.EnrollFingerprintAsync();

                if (template == null || imageData == null)
                {
                    CaptureStatus = $"❌ No se pudo registrar {finger.ToDisplayName()}. Intente de nuevo.";
                    CaptureStatusColor = "#FF7675";
                    return;
                }

                // Show preview
                var (w, h) = _fingerprintService.GetImageSize();
                if (w > 0 && h > 0 && imageData.Length >= w * h)
                {
                    FingerprintImage = FingerprintImageConverter.CreateBitmapFromGrayscale(imageData, w, h);
                }

                // Store for this finger
                _enrolledFingers[finger] = template;
                RefreshEnrolledUI();

                CaptureStatus = $"✅ {finger.ToDisplayName()} registrado — {EnrolledCount} huella(s) total";
                CaptureStatusColor = "#00B894";
                Console.WriteLine($"UI: ✅ {finger.ToDisplayName()} enrollado ({template.Length} bytes).");
            }
            catch (Exception ex)
            {
                CaptureStatus = $"❌ Error: {ex.Message}";
                CaptureStatusColor = "#FF7675";
            }
            finally
            {
                IsCapturing = false;
            }
        }

        [RelayCommand]
        private void RemoveFinger()
        {
            if (!SelectedFinger.HasValue) return;
            var finger = SelectedFinger.Value;

            if (_enrolledFingers.Remove(finger))
            {
                RefreshEnrolledUI();
                CaptureStatus = $"🗑️ {finger.ToDisplayName()} eliminado — {EnrolledCount} huella(s) total";
                CaptureStatusColor = "#8B949E";
                FingerprintImage = null;
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                ValidationMessage = "El nombre es requerido.";
                return;
            }
            if (string.IsNullOrWhiteSpace(DocumentId))
            {
                ValidationMessage = "La cédula es requerida.";
                return;
            }
            if (_enrolledFingers.Count == 0)
            {
                ValidationMessage = "Debe registrar al menos una huella.";
                return;
            }

            ValidationMessage = "";

            try
            {
                // Use the first enrolled finger as the legacy template
                byte[] primaryTemplate = _enrolledFingers.Values.First();

                if (IsEditing && _editingEmployeeId.HasValue)
                {
                    await _attendanceService.UpdateEmployeeAsync(
                        _editingEmployeeId.Value, FullName.Trim(), DocumentId.Trim(), primaryTemplate);

                    // Save all fingerprints
                    await _attendanceService.SaveEmployeeFingerprintsAsync(_editingEmployeeId.Value, _enrolledFingers);
                }
                else
                {
                    var employee = await _attendanceService.RegisterEmployeeAsync(
                        FullName.Trim(), DocumentId.Trim(), primaryTemplate);

                    // Save all fingerprints
                    await _attendanceService.SaveEmployeeFingerprintsAsync(employee.Id, _enrolledFingers);
                }

                SaveCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Error al guardar: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
