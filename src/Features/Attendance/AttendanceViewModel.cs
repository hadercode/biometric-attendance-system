using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LectorHuellas.Core.Models;
using LectorHuellas.Core.Services;

namespace LectorHuellas.Features.Attendance
{
    public partial class AttendanceViewModel : ObservableObject, IDisposable
    {
        private readonly IFingerprintService _fingerprintService;
        private readonly AttendanceService _attendanceService;
        private readonly DispatcherTimer _clockTimer;
        private CancellationTokenSource? _scanningCts;
        private bool _disposed;

        [ObservableProperty]
        private string _currentTime = string.Empty;

        [ObservableProperty]
        private string _currentDate = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Coloque su dedo en el lector para marcar asistencia";

        [ObservableProperty]
        private Color _statusMessageColor = Colors.White;

        [ObservableProperty]
        private Employee? _identifiedEmployee;

        [ObservableProperty]
        private BitmapSource? _fingerprintImage;

        [ObservableProperty]
        private bool _isCapturing;

        [ObservableProperty]
        private bool _showCard;

        [ObservableProperty]
        private string _attendanceTypeBadge = string.Empty;

        [ObservableProperty]
        private ObservableCollection<AttendanceRecord> _recentRecords = new();

        public event EventHandler? AdminAccessRequested;

        public AttendanceViewModel(IFingerprintService fingerprintService, AttendanceService attendanceService)
        {
            _fingerprintService = fingerprintService;
            _attendanceService = attendanceService;
            
            // Setup Clock Timer
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) => UpdateDateTime();
            _clockTimer.Start();
            UpdateDateTime();

            // Start the automatic detection loop
            StartScanning();
        }

        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            CurrentTime = now.ToString("HH:mm:ss");
            CurrentDate = now.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES")).ToUpper();
        }

        public void StartScanning()
        {
            // Ensure any previous loop is stopped
            StopScanning();
            
            _scanningCts = new CancellationTokenSource();
            _ = RunScanningLoopAsync(_scanningCts.Token);
        }

        public void StopScanning()
        {
            _scanningCts?.Cancel();
            _scanningCts = null;
            _fingerprintService.CancelCurrentOperation();
        }

        private async Task RunScanningLoopAsync(CancellationToken ct)
        {
            StatusMessage = "Sistema listo. Coloque su dedo para marcar.";
            StatusMessageColor = Colors.White;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // If we are currently showing a success card, we wait a bit before allowing another scan
                    // or we can allow it immediately. For better UX, let's wait 1 second if a card is visible.
                    if (ShowCard) await Task.Delay(1000, ct);

                    // Reset state before new scan to avoid showing stale data
                    IdentifiedEmployee = null;
                    IsCapturing = true;
                    
                    // Note: IdentifyAndRecordAsync waits inside the SDK for a finger to be placed
                    var result = await _attendanceService.IdentifyAndRecordAsync();

                    if (result.HasValue && !result.Value.NoMatch)
                    {
                        if (result.Value.IsCooldownActive)
                        {
                            // Cooldown active: Avoid duplicate recording
                            ShowCard = false;
                            StatusMessage = $"{result.Value.Employee.FirstNames}, usted ya ha chequeado.";
                            StatusMessageColor = (Color)ColorConverter.ConvertFromString("#F39C12"); // Warning Orange
                            
                            await Task.Delay(3000, ct); // Show warning for 3s
                            
                            StatusMessage = "Listo. Coloque su dedo para marcar.";
                            StatusMessageColor = Colors.White;
                        }
                        else
                        {
                            // Successful new recording
                            ShowCard = false; // Briefly hide to trigger animation/refresh if needed
                            IdentifiedEmployee = result.Value.Employee;
                            AttendanceTypeBadge = result.Value.Type == AttendanceType.CheckIn ? "ENTRADA REGISTRADA" : "SALIDA REGISTRADA";
                            StatusMessage = $"¡Hola {result.Value.Employee.FirstNames}!";
                            StatusMessageColor = (Color)ColorConverter.ConvertFromString("#00B894"); // Success
                            ShowCard = true;

                            await RefreshHistoryAsync();

                            // Auto-hide card after 4 seconds, but don't block the loop
                            _ = ResetSuccessStateAfterDelay(4000);
                        }
                    }
                    else if (result.HasValue && result.Value.NoMatch)
                    {
                        // Match failed but NOT cancelled
                        ShowCard = false;
                        StatusMessage = "Huella no reconocida. Por favor, coloque el dedo nuevamente.";
                        StatusMessageColor = (Color)ColorConverter.ConvertFromString("#FF7675"); // Danger
                        await Task.Delay(2500, ct); // Show error for 2.5s
                        
                        StatusMessage = "Listo. Coloque su dedo para marcar.";
                        StatusMessageColor = Colors.White;
                    }
                    else
                    {
                        // result is null: probably cancelled or error
                        // Don't show error if we are stopping
                        if (ct.IsCancellationRequested) break;
                        
                        // Small delay if it was a transient error
                        if (!ct.IsCancellationRequested) await Task.Delay(500, ct);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    ShowCard = false;
                    StatusMessage = $"Error: {ex.Message}. Intente de nuevo.";
                    StatusMessageColor = (Color)ColorConverter.ConvertFromString("#FF7675");
                    await Task.Delay(3000, ct); // Wait before retrying
                }
                finally
                {
                    IsCapturing = false;
                }
            }
        }

        private async Task ResetSuccessStateAfterDelay(int delayMs)
        {
            await Task.Delay(delayMs);
            if (!_disposed)
            {
                ShowCard = false;
                
                // Wait for animation to finish before clearing data
                await Task.Delay(500);
                IdentifiedEmployee = null;
                StatusMessage = "Listo. Coloque su dedo para marcar.";
                StatusMessageColor = Colors.White;
            }
        }

        public async Task RefreshHistoryAsync()
        {
            try
            {
                var records = await _attendanceService.GetRecentRecordsAsync(10);
                RecentRecords.Clear();
                foreach (var r in records) RecentRecords.Add(r);
            }
            catch { /* Ignore */ }
        }

        [RelayCommand]
        public async Task CaptureAndIdentifyAsync()
        {
            if (IsCapturing) return;

            try
            {
                IsCapturing = true;
                ShowCard = false;
                StatusMessage = "Inicie captura y coloque el dedo...";
                StatusMessageColor = (Color)ColorConverter.ConvertFromString("#00CEC9"); // Accent

                // Unified identification and recording
                var result = await _attendanceService.IdentifyAndRecordAsync();
                
                if (result.HasValue && !result.Value.NoMatch)
                {
                    if (result.Value.IsCooldownActive)
                    {
                        StatusMessage = $"{result.Value.Employee.FirstNames}, usted ya ha chequeado.";
                        StatusMessageColor = (Color)ColorConverter.ConvertFromString("#F39C12"); // Warning Orange
                    }
                    else
                    {
                        IdentifiedEmployee = result.Value.Employee;
                        AttendanceTypeBadge = result.Value.Type == AttendanceType.CheckIn ? "ENTRADA REGISTRADA" : "SALIDA REGISTRADA";
                        StatusMessage = "¡Bienvenido!";
                        StatusMessageColor = (Color)ColorConverter.ConvertFromString("#00B894"); // Success
                        ShowCard = true;

                        await RefreshHistoryAsync();

                        // Auto-hide card after 5 seconds
                        _ = Task.Run(async () => {
                            await Task.Delay(5000);
                            ShowCard = false;
                            await Task.Delay(500); // Animation delay
                            
                            await App.Current.Dispatcher.InvokeAsync(() => {
                                IdentifiedEmployee = null;
                                StatusMessage = "Coloque su dedo en el lector para marcar asistencia";
                                StatusMessageColor = Colors.White;
                            });
                        });
                    }
                }
                else if (result.HasValue && result.Value.NoMatch)
                {
                    StatusMessage = "Huella no reconocida. Intente de nuevo.";
                    StatusMessageColor = (Color)ColorConverter.ConvertFromString("#FF7675"); // Danger
                }
                else
                {
                    // Cancelled or transient error
                    StatusMessage = "Captura cancelada.";
                    StatusMessageColor = Colors.White;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageColor = (Color)ColorConverter.ConvertFromString("#FF7675");
            }
            finally
            {
                IsCapturing = false;
            }
        }

        [RelayCommand]
        private void RequestAdminAccess()
        {
            // We don't stop the loop here, as Login is a dialog.
            // But we might want to pause if needed.
            AdminAccessRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _clockTimer.Stop();
                _scanningCts?.Cancel();
                _scanningCts?.Dispose();
                _disposed = true;
            }
        }
    }
}
