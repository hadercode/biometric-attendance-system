using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LectorHuellas.Shared.Converters;
using LectorHuellas.Core.Models;
using LectorHuellas.Core.Services;

namespace LectorHuellas.Features.Employees
{
    public partial class EmployeeFormViewModel : ObservableObject
    {
        private readonly IFingerprintService _fingerprintService;
        private readonly IEmployeeService _employeeService;
        private readonly ICommonService _commonService;
        private readonly AttendanceService _attendanceService;
        private readonly Dispatcher _dispatcher;

        private int? _editingEmployeeId;

        private readonly Dictionary<FingerType, byte[]> _enrolledFingers = new();

        [ObservableProperty]
        private string _code = "";

        [ObservableProperty]
        private string _firstNames = "";

        [ObservableProperty]
        private string _lastNames = "";

        [ObservableProperty]
        private string _position = ""; 

        [ObservableProperty]
        private string _managementId = "";

        [ObservableProperty]
        private string _departmentId = "";

        [ObservableProperty]
        private string _unitId = "";

        [ObservableProperty]
        private string _shiftId = "";

        [ObservableProperty]
        private string _address = "";

        [ObservableProperty]
        private string _phone = "";

        [ObservableProperty]
        private DateTime? _birthDate;

        [ObservableProperty]
        private DateTime? _hireDate = DateTime.Now;

        [ObservableProperty]
        private string _message = "";

        [ObservableProperty]
        private string _photoPath = ""; 

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

        // Master Data Collections
        [ObservableProperty]
        private ObservableCollection<Management> _managements = new();

        [ObservableProperty]
        private ObservableCollection<Department> _departments = new();

        [ObservableProperty]
        private ObservableCollection<Unit> _units = new();

        [ObservableProperty]
        private ObservableCollection<Shift> _shifts = new();

        public event EventHandler? SaveCompleted;

        public EmployeeFormViewModel(IFingerprintService fingerprintService, IEmployeeService employeeService, ICommonService commonService, AttendanceService attendanceService)
        {
            _fingerprintService = fingerprintService;
            _employeeService = employeeService;
            _commonService = commonService;
            _attendanceService = attendanceService;
            _dispatcher = Dispatcher.CurrentDispatcher;

            _fingerprintService.OnStatusMessage += msg =>
            {
                _dispatcher.Invoke(() =>
                {
                    CaptureStatus = msg;
                    CaptureStatusColor = "#FDCB6E";
                });
            };

            // Load master data
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            Console.WriteLine("DEBUG: EmployeeFormViewModel.InitializeAsync starting...");
            try
            {
                var mgrs = await _commonService.GetManagementsAsync();
                var dpts = await _commonService.GetDepartmentsAsync();
                var units = await _commonService.GetUnitsAsync();
                var shifts = await _commonService.GetShiftsAsync();

                Console.WriteLine($"DEBUG: Data loaded - Mgrs: {mgrs.Count}, Dpts: {dpts.Count}, Units: {units.Count}, Shifts: {shifts.Count}");

                _dispatcher.Invoke(() => {
                    Managements = new ObservableCollection<Management>(mgrs);
                    Departments = new ObservableCollection<Department>(dpts);
                    Units = new ObservableCollection<Unit>(units);
                    Shifts = new ObservableCollection<Shift>(shifts);
                    Console.WriteLine("DEBUG: ObservableCollections updated in ViewModel.");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error initializing form data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error initializing form data: {ex.Message}");
            }
        }

        public void Clear()
        {
            _editingEmployeeId = null;
            Code = "";
            FirstNames = "";
            LastNames = "";
            Position = "";
            ManagementId = "";
            DepartmentId = "";
            UnitId = "";
            ShiftId = "";
            Address = "";
            Phone = "";
            BirthDate = null;
            HireDate = DateTime.Now;
            Message = "";
            PhotoPath = "";
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
            Code = employee.Code;
            FirstNames = employee.FirstNames;
            LastNames = employee.LastNames;
            Position = employee.PositionId;
            ManagementId = employee.ManagementId;
            DepartmentId = employee.DepartmentId;
            UnitId = employee.UnitId;
            ShiftId = employee.ShiftId;
            Address = employee.Address;
            Phone = employee.Phone;
            BirthDate = employee.BirthDate;
            HireDate = employee.HireDate;
            Message = employee.Message;
            PhotoPath = employee.PhotoPath ?? "";
            FormTitle = "Editar Empleado";
            IsEditing = true;
            FingerprintImage = null;
            ValidationMessage = "";

            _enrolledFingers.Clear();
            var fingerprints = await _employeeService.GetEmployeeFingerprintsAsync(employee.Id);
            foreach (var fp in fingerprints)
            {
                _enrolledFingers[fp.FingerType] = fp.TemplateData;
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
                var (imageData, template) = await _fingerprintService.EnrollFingerprintAsync();

                if (template == null || imageData == null)
                {
                    CaptureStatus = $"❌ No se pudo registrar {finger.ToDisplayName()}. Intente de nuevo.";
                    CaptureStatusColor = "#FF7675";
                    return;
                }

                var (w, h) = _fingerprintService.GetImageSize();
                if (w > 0 && h > 0 && imageData.Length >= w * h)
                {
                    FingerprintImage = FingerprintImageConverter.CreateBitmapFromGrayscale(imageData, w, h);
                }

                _enrolledFingers[finger] = template;
                RefreshEnrolledUI();

                CaptureStatus = $"✅ {finger.ToDisplayName()} registrado — {EnrolledCount} huella(s) total";
                CaptureStatusColor = "#00B894";
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
        private void SelectPhoto()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Seleccionar Foto del Empleado"
            };

            if (dialog.ShowDialog() == true)
            {
                PhotoPath = dialog.FileName;
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                ValidationMessage = "El código (cedula) es requerido.";
                return;
            }
            if (string.IsNullOrWhiteSpace(FirstNames))
            {
                ValidationMessage = "Los nombres son requeridos.";
                return;
            }
            if (string.IsNullOrWhiteSpace(LastNames))
            {
                ValidationMessage = "Los apellidos son requeridos.";
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
                if (IsEditing && _editingEmployeeId.HasValue)
                {
                    await _employeeService.UpdateEmployeeAsync(
                        _editingEmployeeId.Value, 
                        Code.Trim(), 
                        FirstNames.Trim(), 
                        LastNames.Trim(), 
                        Position.Trim(),
                        ManagementId,
                        DepartmentId,
                        UnitId,
                        ShiftId,
                        PhotoPath,
                        Address,
                        Phone,
                        BirthDate,
                        HireDate,
                        Message);

                    await _employeeService.SaveEmployeeFingerprintsAsync(_editingEmployeeId.Value, _enrolledFingers);
                }
                else
                {
                    byte[] primaryTemplate = _enrolledFingers.Values.First();
                    
                    var employee = await _employeeService.RegisterEmployeeAsync(
                        Code.Trim(), 
                        FirstNames.Trim(), 
                        LastNames.Trim(), 
                        Position.Trim(),
                        ManagementId,
                        DepartmentId,
                        UnitId,
                        ShiftId,
                        PhotoPath, 
                        Address,
                        Phone,
                        BirthDate,
                        HireDate,
                        Message,
                        primaryTemplate);

                    await _employeeService.SaveEmployeeFingerprintsAsync(employee.Id, _enrolledFingers);
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
