using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LectorHuellas.Models;
using LectorHuellas.Services;

namespace LectorHuellas.ViewModels
{
    public partial class EmployeeListViewModel : ObservableObject
    {
        private readonly AttendanceService _attendanceService;

        [ObservableProperty]
        private ObservableCollection<Employee> _employees = new();

        [ObservableProperty]
        private Employee? _selectedEmployee;

        [ObservableProperty]
        private bool _isLoading;

        public event EventHandler<Employee>? EditEmployeeRequested;

        public EmployeeListViewModel(AttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [RelayCommand]
        private async Task Refresh()
        {
            IsLoading = true;
            try
            {
                var list = await _attendanceService.GetAllEmployeesAsync();
                Employees = new ObservableCollection<Employee>(list);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading employees: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void EditEmployee()
        {
            if (SelectedEmployee != null)
            {
                EditEmployeeRequested?.Invoke(this, SelectedEmployee);
            }
        }

        [RelayCommand]
        private async Task DeleteEmployee()
        {
            if (SelectedEmployee == null) return;

            var result = System.Windows.MessageBox.Show(
                $"¿Está seguro de eliminar a {SelectedEmployee.FullName}?",
                "Confirmar eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _attendanceService.DeleteEmployeeAsync(SelectedEmployee.Id);
                await Refresh();
            }
        }
    }
}
