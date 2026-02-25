using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LectorHuellas.Services;

namespace LectorHuellas.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IFingerprintService _fingerprintService;
        private readonly AttendanceService _attendanceService;

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private string _currentPage = "Dashboard";

        [ObservableProperty]
        private string _deviceStatus = "Desconectado";

        [ObservableProperty]
        private bool _isDeviceConnected;

        [ObservableProperty]
        private bool _isSimulated;

        // Child ViewModels
        public DashboardViewModel DashboardVM { get; }
        public EmployeeListViewModel EmployeeListVM { get; }
        public EmployeeFormViewModel EmployeeFormVM { get; }
        public AttendanceReportViewModel AttendanceReportVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public MainViewModel(IFingerprintService fingerprintService, AttendanceService attendanceService)
        {
            _fingerprintService = fingerprintService;
            _attendanceService = attendanceService;

            DashboardVM = new DashboardViewModel(fingerprintService, attendanceService);
            EmployeeListVM = new EmployeeListViewModel(attendanceService);
            EmployeeFormVM = new EmployeeFormViewModel(fingerprintService, attendanceService);
            AttendanceReportVM = new AttendanceReportViewModel(attendanceService);
            SettingsVM = new SettingsViewModel();

            IsDeviceConnected = fingerprintService.IsDeviceConnected;
            IsSimulated = fingerprintService.IsSimulated;
            DeviceStatus = IsSimulated ? "🔧 Modo Simulado" :
                           IsDeviceConnected ? "✅ FS80H Conectado" : "❌ Desconectado";

            // Wire up navigation events
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
    }
}
