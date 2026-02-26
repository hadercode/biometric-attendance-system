using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LectorHuellas.Core.Services;
using LectorHuellas.Features.Attendance;
using LectorHuellas.Features.Dashboard;
using LectorHuellas.Features.Employees;
using LectorHuellas.Features.Auth;
using LectorHuellas.Features.Reports;
using LectorHuellas.Features.Settings;

namespace LectorHuellas.Features.Main
{
    public enum AppMode
    {
        Public, // Attendance ID Card
        Auth,   // Login Screen
        Admin   // Dashboard & Management
    }

    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly IFingerprintService _fingerprintService;
        private readonly IEmployeeService _employeeService;
        private readonly ICommonService _commonService;
        private readonly IThemeService _themeService;
        private readonly AttendanceService _attendanceService;
        private readonly IAuthService _authService;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPublicMode))]
        [NotifyPropertyChangedFor(nameof(IsAuthMode))]
        [NotifyPropertyChangedFor(nameof(IsAdminMode))]
        private AppMode _currentMode = AppMode.Public;

        public bool IsPublicMode => CurrentMode == AppMode.Public;
        public bool IsAuthMode => CurrentMode == AppMode.Auth;
        public bool IsAdminMode => CurrentMode == AppMode.Admin;

        public bool CanConfigureSettings => _sessionService.CanConfigureSettings;
        public string UserDisplayName => _sessionService.CurrentUser?.FullName ?? "Usuario";
        public int CurrentUserRoleId => _sessionService.CurrentUser?.RolId ?? 0;

        [ObservableProperty]
        private string _currentPage = "Attendance";

        [ObservableProperty]
        private string _deviceStatus = "Desconectado";

        [ObservableProperty]
        private bool _isDeviceConnected;

        [ObservableProperty]
        private bool _isSimulated;

        private readonly DispatcherTimer _connectionTimer;
        private bool _isPollerActive = true;

        [ObservableProperty]
        private bool _isScannerDetected;

        // Child ViewModels
        public AttendanceViewModel AttendanceVM { get; }
        public LoginViewModel LoginVM { get; }
        public DashboardViewModel DashboardVM { get; }
        public EmployeeListViewModel EmployeeListVM { get; }
        public EmployeeFormViewModel EmployeeFormVM { get; }
        public AttendanceReportViewModel AttendanceReportVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public bool IsDarkTheme => _themeService.IsDarkTheme;

        public MainViewModel(IFingerprintService fingerprintService, IEmployeeService employeeService, ICommonService commonService, 
            IThemeService themeService, AttendanceService attendanceService, IAuthService authService, SessionService sessionService)
        {
            _fingerprintService = fingerprintService;
            _employeeService = employeeService;
            _commonService = commonService;
            _themeService = themeService;
            _attendanceService = attendanceService;
            _authService = authService;
            _sessionService = sessionService;

            // Initialize all child ViewModels
            AttendanceVM = new AttendanceViewModel(fingerprintService, attendanceService);
            LoginVM = new LoginViewModel(authService, sessionService);
            DashboardVM = new DashboardViewModel(fingerprintService, employeeService, attendanceService);
            EmployeeListVM = new EmployeeListViewModel(employeeService, commonService);
            EmployeeFormVM = new EmployeeFormViewModel(fingerprintService, employeeService, commonService, attendanceService);
            AttendanceReportVM = new AttendanceReportViewModel(employeeService, attendanceService);
            SettingsVM = new SettingsViewModel();

            IsDeviceConnected = fingerprintService.IsDeviceConnected;
            IsSimulated = fingerprintService.IsSimulated;
            IsScannerDetected = IsDeviceConnected;
            UpdateDeviceStatus();

            // Initialize Attendance (only history)
            _ = AttendanceVM.RefreshHistoryAsync();

            // Wire up Attendance events
            AttendanceVM.AdminAccessRequested += (s, e) => 
            {
                CurrentMode = AppMode.Auth;
                CurrentPage = "Login";
            };

            // Wire up Auth events
            LoginVM.LoginSuccess += (s, e) =>
            {
                CurrentMode = AppMode.Admin;
                NavigateToPage("Dashboard");
                LoginVM.Clear();
                OnPropertyChanged(nameof(CanConfigureSettings));
                OnPropertyChanged(nameof(UserDisplayName));
            };

            LoginVM.BackRequested += (s, e) =>
            {
                CurrentMode = AppMode.Public;
                CurrentPage = "Attendance";
                LoginVM.Clear();
                _ = AttendanceVM.RefreshHistoryAsync();
            };

            // Wire up Employee management events
            EmployeeListVM.EditEmployeeRequested += (_, emp) =>
            {
                EmployeeFormVM.LoadEmployee(emp);
                NavigateToPage("EmployeeForm");
            };

            EmployeeFormVM.SaveCompleted += (_, __) =>
            {
                NavigateToPage("Employees");
            };

            // Setup connection poller
            _connectionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _connectionTimer.Tick += ConnectionTimer_Tick;
            _connectionTimer.Start();
        }

        private void ConnectionTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isPollerActive) return;

            // We check for hardware regardless of being in simulation mode
            // This allows auto-detecting plug-ins as well as disconnections
            bool isHardwarePresent = FutronicService.StaticCheckPresence();
            
            // Handle Disconnection
            if (IsScannerDetected && !isHardwarePresent)
            {
                Console.WriteLine("⚠️ Lector desconectado detectado.");
                IsDeviceConnected = false;
                IsScannerDetected = false;
                UpdateDeviceStatus();
            }
            // Handle Plug-in (Hardware appeared)
            else if (!IsScannerDetected && isHardwarePresent)
            {
                Console.WriteLine("🔌 Lector detectado (en espera de activación).");
                IsScannerDetected = true;
                UpdateDeviceStatus();
            }
        }

        [RelayCommand]
        private void NavigateToPage(string page)
        {
            if (page == "Settings" && !CanConfigureSettings)
            {
                // Prevent navigation if not allowed
                return;
            }

            CurrentPage = page;

            if (page == "Employees")
            {
                EmployeeListVM.RefreshCommand.Execute(null);
            }
            else if (page == "EmployeeForm")
            {
                // Already handled by the edit event or new employee
            }
            else if (page == "Reports")
            {
                AttendanceReportVM.SearchCommand.Execute(null);
            }
            else if (page == "Dashboard")
            {
                DashboardVM.RefreshStatsCommand.Execute(null);
            }
        }

        [RelayCommand]
        private void NewEmployee()
        {
            EmployeeFormVM.Clear();
            NavigateToPage("EmployeeForm");
        }

        [RelayCommand]
        private void Logout()
        {
            _sessionService.ClearSession();
            CurrentMode = AppMode.Public;
            CurrentPage = "Attendance";
            _ = AttendanceVM.RefreshHistoryAsync();
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            _themeService.ToggleTheme();
            OnPropertyChanged(nameof(IsDarkTheme));
        }

        [RelayCommand]
        private void RetryScannerDetection()
        {
            Console.WriteLine("🔄 Reintentando detección del lector...");
            
            // 1. Try to create and initialize a real Futronic service
            var realService = new FutronicService();
            if (realService.Initialize())
            {
                Console.WriteLine("✅ Lector detectado y activado!");
                
                // 2. Swap the service in the proxy
                if (_fingerprintService is ScannerProxyService proxy)
                {
                    proxy.SetInternalService(realService);
                }
                
                IsDeviceConnected = true;
                IsSimulated = false;
                IsScannerDetected = true;
            }
            else
            {
                Console.WriteLine("❌ No se encontró el lector real.");
                realService.Dispose();
                
                IsDeviceConnected = false;
                IsScannerDetected = false;
                // Stay in simulated mode if that was the state
            }
            
            UpdateDeviceStatus();
            
            // Restart scanning if we are in public mode
            if (IsPublicMode)
            {
                AttendanceVM.StartScanning();
            }
        }

        private void UpdateDeviceStatus()
        {
            if (IsDeviceConnected && !IsSimulated)
            {
                DeviceStatus = "✅ Conectado";
            }
            else if (!IsScannerDetected)
            {
                DeviceStatus = "❌ No detectado";
            }
            else if (IsSimulated)
            {
                // In simulated mode but hardware is present (awaiting activation)
                DeviceStatus = "🔌 Pendiente (🔄)";
            }
            else
            {
                DeviceStatus = "❌ Desconectado";
            }
            
            // Force property notification for UI triggers
            OnPropertyChanged(nameof(IsScannerDetected));
        }

        public void Dispose()
        {
            _connectionTimer?.Stop();
            AttendanceVM.Dispose();
        }
    }
}
