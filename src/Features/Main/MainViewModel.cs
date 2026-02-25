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
        private readonly AttendanceService _attendanceService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPublicMode))]
        [NotifyPropertyChangedFor(nameof(IsAuthMode))]
        [NotifyPropertyChangedFor(nameof(IsAdminMode))]
        private AppMode _currentMode = AppMode.Public;

        public bool IsPublicMode => CurrentMode == AppMode.Public;
        public bool IsAuthMode => CurrentMode == AppMode.Auth;
        public bool IsAdminMode => CurrentMode == AppMode.Admin;

        [ObservableProperty]
        private string _currentPage = "Attendance";

        [ObservableProperty]
        private string _deviceStatus = "Desconectado";

        [ObservableProperty]
        private bool _isDeviceConnected;

        [ObservableProperty]
        private bool _isSimulated;

        // Child ViewModels
        public AttendanceViewModel AttendanceVM { get; }
        public LoginViewModel LoginVM { get; }
        public DashboardViewModel DashboardVM { get; }
        public EmployeeListViewModel EmployeeListVM { get; }
        public EmployeeFormViewModel EmployeeFormVM { get; }
        public AttendanceReportViewModel AttendanceReportVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public MainViewModel(IFingerprintService fingerprintService, IEmployeeService employeeService, ICommonService commonService, AttendanceService attendanceService)
        {
            _fingerprintService = fingerprintService;
            _employeeService = employeeService;
            _commonService = commonService;
            _attendanceService = attendanceService;

            // Initialize all child ViewModels
            AttendanceVM = new AttendanceViewModel(fingerprintService, attendanceService);
            LoginVM = new LoginViewModel();
            DashboardVM = new DashboardViewModel(fingerprintService, employeeService, attendanceService);
            EmployeeListVM = new EmployeeListViewModel(employeeService);
            EmployeeFormVM = new EmployeeFormViewModel(fingerprintService, employeeService, commonService, attendanceService);
            AttendanceReportVM = new AttendanceReportViewModel(employeeService, attendanceService);
            SettingsVM = new SettingsViewModel();

            IsDeviceConnected = fingerprintService.IsDeviceConnected;
            IsSimulated = fingerprintService.IsSimulated;
            DeviceStatus = IsSimulated ? "🔧 Modo Simulado" :
                           IsDeviceConnected ? "✅ FS80H Conectado" : "❌ Desconectado";

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
        }

        [RelayCommand]
        private void NavigateToPage(string page)
        {
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
            CurrentMode = AppMode.Public;
            CurrentPage = "Attendance";
            _ = AttendanceVM.RefreshHistoryAsync();
        }

        public void Dispose()
        {
            AttendanceVM.Dispose();
        }
    }
}
