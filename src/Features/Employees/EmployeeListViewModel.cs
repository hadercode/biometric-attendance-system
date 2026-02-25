using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LectorHuellas.Core.Models;
using LectorHuellas.Core.Services;

namespace LectorHuellas.Features.Employees
{
    public partial class EmployeeListViewModel : ObservableObject
    {
        private readonly IEmployeeService _employeeService;

        [ObservableProperty]
        private ObservableCollection<Employee> _employees = new();

        [ObservableProperty]
        private Employee? _selectedEmployee;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 20;

        [ObservableProperty]
        private int _totalRecords;

        [ObservableProperty]
        private int _totalPages;

        [ObservableProperty]
        private bool _canGoNext;

        [ObservableProperty]
        private bool _canGoPrevious;

        [ObservableProperty]
        private string _searchText = string.Empty;

        public event EventHandler<Employee>? EditEmployeeRequested;

        public EmployeeListViewModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [RelayCommand]
        private async Task Refresh()
        {
            if (IsLoading) return; // Re-entry guard
            
            IsLoading = true;
            try
            {
                TotalRecords = await _employeeService.GetTotalEmployeesCountAsync(SearchText);
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
                if (TotalPages == 0) TotalPages = 1;

                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                if (CurrentPage < 1) CurrentPage = 1;

                var list = await _employeeService.GetEmployeesPaginatedAsync(CurrentPage, PageSize, SearchText);
                Employees = new ObservableCollection<Employee>(list);

                UpdateNavigationState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR en Refresh: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await Refresh();
            }
        }

        [RelayCommand]
        private async Task PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await Refresh();
            }
        }

        private void UpdateNavigationState()
        {
            CanGoNext = CurrentPage < TotalPages;
            CanGoPrevious = CurrentPage > 1;
        }

        partial void OnSearchTextChanged(string value)
        {
            CurrentPage = 1; // Reset to page 1 on search
            _ = Refresh();
        }

        [RelayCommand]
        private void EditEmployee(Employee? employee)
        {
            var target = employee ?? SelectedEmployee;
            if (target != null)
            {
                EditEmployeeRequested?.Invoke(this, target);
            }
        }

        [RelayCommand]
        private async Task DeleteEmployee(Employee? employee)
        {
            var target = employee ?? SelectedEmployee;
            if (target == null) return;

            // Check for attendance history first
            var hasHistory = await _employeeService.HasAttendanceRecordsAsync(target.Id);

            if (hasHistory)
            {
                var deactivateResult = System.Windows.MessageBox.Show(
                    $"{target.FullName} tiene historial de asistencia y no puede ser eliminado.\n\n¿Desea desactivar al empleado en su lugar?",
                    "No se puede eliminar",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);

                if (deactivateResult == System.Windows.MessageBoxResult.Yes)
                {
                    await _employeeService.DeactivateEmployeeAsync(target.Id);
                    await Refresh();
                }
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"¿Está seguro de eliminar a {target.FullName}?",
                "Confirmar eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _employeeService.DeleteEmployeeAsync(target.Id);
                await Refresh();
            }
        }
    }
}
