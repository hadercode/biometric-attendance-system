using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LectorHuellas.Core.Models;
using LectorHuellas.Core.Services;

namespace LectorHuellas.Features.Reports
{
    public partial class AttendanceReportViewModel : ObservableObject
    {
        private readonly IEmployeeService _employeeService;
        private readonly AttendanceService _attendanceService;

        [ObservableProperty]
        private DateTime _dateFrom = DateTime.Today;

        [ObservableProperty]
        private DateTime _dateTo = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<AttendanceRecord> _records = new();

        [ObservableProperty]
        private ObservableCollection<Employee> _employees = new();

        [ObservableProperty]
        private Employee? _selectedEmployee;

        [ObservableProperty]
        private int _totalRecords;

        [ObservableProperty]
        private int _checkInCount;

        [ObservableProperty]
        private int _checkOutCount;

        [ObservableProperty]
        private bool _isLoading;

        public AttendanceReportViewModel(IEmployeeService employeeService, AttendanceService attendanceService)
        {
            _employeeService = employeeService;
            _attendanceService = attendanceService;
        }

        [RelayCommand]
        private async Task Search()
        {
            IsLoading = true;
            try
            {
                // Load employee filter list from EmployeeService
                var empList = await _employeeService.GetAllEmployeesAsync();
                Employees = new ObservableCollection<Employee>(empList);

                // Load records from AttendanceService
                var records = await _attendanceService.GetAttendanceReportAsync(
                    DateFrom, DateTo, SelectedEmployee?.Id);

                Records = new ObservableCollection<AttendanceRecord>(records);
                TotalRecords = records.Count;
                CheckInCount = records.Count(r => r.Type == AttendanceType.CheckIn);
                CheckOutCount = records.Count(r => r.Type == AttendanceType.CheckOut);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching attendance: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void FilterToday()
        {
            DateFrom = DateTime.Today;
            DateTo = DateTime.Today;
            SearchCommand.Execute(null);
        }

        [RelayCommand]
        private void FilterWeek()
        {
            DateFrom = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            DateTo = DateTime.Today;
            SearchCommand.Execute(null);
        }

        [RelayCommand]
        private void FilterMonth()
        {
            DateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTo = DateTime.Today;
            SearchCommand.Execute(null);
        }
    }
}
