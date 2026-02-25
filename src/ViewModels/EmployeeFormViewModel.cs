using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
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

        private int? _editingEmployeeId;
        private byte[]? _capturedTemplate;

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
        private string _captureStatus = "Sin huella capturada";

        [ObservableProperty]
        private string _captureStatusColor = "#8B949E";

        [ObservableProperty]
        private BitmapSource? _fingerprintImage;

        [ObservableProperty]
        private string _validationMessage = "";

        public event EventHandler? SaveCompleted;

        public EmployeeFormViewModel(IFingerprintService fingerprintService, AttendanceService attendanceService)
        {
            _fingerprintService = fingerprintService;
            _attendanceService = attendanceService;
        }

        public void Clear()
        {
            _editingEmployeeId = null;
            FullName = "";
            DocumentId = "";
            FormTitle = "Nuevo Empleado";
            IsEditing = false;
            HasFingerprint = false;
            _capturedTemplate = null;
            CaptureStatus = "Sin huella capturada";
            CaptureStatusColor = "#8B949E";
            FingerprintImage = null;
            ValidationMessage = "";
        }

        public void LoadEmployee(Employee employee)
        {
            _editingEmployeeId = employee.Id;
            FullName = employee.FullName;
            DocumentId = employee.DocumentId;
            FormTitle = "Editar Empleado";
            IsEditing = true;
            HasFingerprint = employee.FingerprintTemplate != null;
            _capturedTemplate = employee.FingerprintTemplate;

            if (HasFingerprint)
            {
                CaptureStatus = "✅ Huella registrada";
                CaptureStatusColor = "#00B894";
            }
            else
            {
                CaptureStatus = "Sin huella capturada";
                CaptureStatusColor = "#8B949E";
            }

            FingerprintImage = null;
            ValidationMessage = "";
        }

        [RelayCommand]
        private async Task CaptureFingerprint()
        {
            if (IsCapturing) return;

            IsCapturing = true;
            CaptureStatus = "⏳ Coloque el dedo en el lector... (enrollment SDK)";
            CaptureStatusColor = "#FDCB6E";
            ValidationMessage = "";

            try
            {
                Console.WriteLine("UI: Iniciando enrollment de huella...");
                var (imageData, template) = await _fingerprintService.EnrollFingerprintAsync();

                if (template == null || imageData == null)
                {
                    CaptureStatus = "❌ No se pudo registrar la huella. Revise la consola.";
                    CaptureStatusColor = "#FF7675";
                    Console.WriteLine("UI: EnrollFingerprintAsync retornó null.");
                    return;
                }

                Console.WriteLine($"UI: Template recibido ({template.Length} bytes). Imagen: {imageData.Length} bytes.");

                // Display image
                var (w, h) = _fingerprintService.GetImageSize();
                if (w > 0 && h > 0 && imageData.Length >= w * h)
                {
                    FingerprintImage = FingerprintImageConverter.CreateBitmapFromGrayscale(imageData, w, h);
                }

                // Store the proper SDK template
                _capturedTemplate = template;
                HasFingerprint = true;
                CaptureStatus = "✅ Huella registrada correctamente (template SDK)";
                CaptureStatusColor = "#00B894";
                Console.WriteLine("UI: ✅ Enrollment completado exitosamente.");
            }
            catch (Exception ex)
            {
                CaptureStatus = $"❌ Error: {ex.Message}";
                CaptureStatusColor = "#FF7675";
                Console.WriteLine($"UI: ❌ Excepción en enrollment: {ex}");
            }
            finally
            {
                IsCapturing = false;
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            // Validation
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
            if (_capturedTemplate == null)
            {
                ValidationMessage = "Debe capturar la huella del empleado.";
                return;
            }

            ValidationMessage = "";

            try
            {
                if (IsEditing && _editingEmployeeId.HasValue)
                {
                    await _attendanceService.UpdateEmployeeAsync(
                        _editingEmployeeId.Value, FullName.Trim(), DocumentId.Trim(), _capturedTemplate);
                }
                else
                {
                    await _attendanceService.RegisterEmployeeAsync(
                        FullName.Trim(), DocumentId.Trim(), _capturedTemplate);
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
