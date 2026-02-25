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
        private readonly ICommonService _commonService;

        [ObservableProperty]
        private ObservableCollection<Department> _departments = new();

        [ObservableProperty]
        private string? _selectedDepartmentId;

        [ObservableProperty]
        private string _nameFilter = string.Empty;

        [ObservableProperty]
        private ObservableCollection<EmployeeDisplayWrapper> _employees = new();

        [ObservableProperty]
        private EmployeeDisplayWrapper? _selectedEmployee;

        [ObservableProperty]
        private string _sortColumn = "LastNames";

        [ObservableProperty]
        private bool _isSortDescending = false;

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

        public EmployeeListViewModel(IEmployeeService employeeService, ICommonService commonService)
        {
            _employeeService = employeeService;
            _commonService = commonService;
            _ = LoadDepartmentsAsync();
        }

        private async Task LoadDepartmentsAsync()
        {
            var deps = await _commonService.GetDepartmentsAsync();
            Departments = new ObservableCollection<Department>(deps);
        }

        [RelayCommand]
        private async Task Refresh()
        {
            if (IsLoading) return; // Re-entry guard
            
            IsLoading = true;
            try
            {
                TotalRecords = await _employeeService.GetTotalEmployeesCountAsync(SearchText, SelectedDepartmentId, NameFilter);
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
                if (TotalPages == 0) TotalPages = 1;

                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                if (CurrentPage < 1) CurrentPage = 1;

                var list = await _employeeService.GetEmployeesPaginatedAsync(CurrentPage, PageSize, SearchText, SortColumn, IsSortDescending, SelectedDepartmentId, NameFilter);
                
                var startNumber = (CurrentPage - 1) * PageSize + 1;
                var displayList = list.Select((e, i) => new EmployeeDisplayWrapper 
                { 
                    Employee = e, 
                    Index = startNumber + i 
                }).ToList();

                Employees = new ObservableCollection<EmployeeDisplayWrapper>(displayList);

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
            CurrentPage = 1;
            _ = Refresh();
        }

        partial void OnSelectedDepartmentIdChanged(string? value)
        {
            CurrentPage = 1;
            _ = Refresh();
        }

        partial void OnNameFilterChanged(string value)
        {
            CurrentPage = 1;
            _ = Refresh();
        }

        [RelayCommand]
        private async Task ClearFilters()
        {
            NameFilter = string.Empty;
            SelectedDepartmentId = null;
            SearchText = string.Empty;
            await Refresh();
        }

        [RelayCommand]
        private async Task Sort(string column)
        {
            if (SortColumn == column)
            {
                IsSortDescending = !IsSortDescending;
            }
            else
            {
                SortColumn = column;
                IsSortDescending = false;
            }
            await Refresh();
        }

        [RelayCommand]
        private void EditEmployee(EmployeeDisplayWrapper? wrapper)
        {
            var target = wrapper?.Employee ?? SelectedEmployee?.Employee;
            if (target != null)
            {
                EditEmployeeRequested?.Invoke(this, target);
            }
        }

        [RelayCommand]
        private async Task DeleteEmployee(EmployeeDisplayWrapper? wrapper)
        {
            var target = wrapper?.Employee ?? SelectedEmployee?.Employee;
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

    public class EmployeeDisplayWrapper
    {
        public Employee Employee { get; set; } = null!;
        public int Index { get; set; }
    }
}
