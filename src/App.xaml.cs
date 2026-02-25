using System;
using System.IO;
using System.Windows;
using LectorHuellas.Data;
using LectorHuellas.Interop;
using LectorHuellas.Services;
using LectorHuellas.ViewModels;

namespace LectorHuellas
{
    public partial class App : Application
    {
        private IFingerprintService? _fingerprintService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AllocConsole();
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine("  LectorHuellas - Diagnóstico de Inicio");
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine($"Directorio: {AppDomain.CurrentDomain.BaseDirectory}");

            // Initialize database
            try
            {
                using var db = new AppDbContext();
                db.EnsureCreated();
                Console.WriteLine("✅ Base de datos inicializada correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error DB: {ex.Message}");
                MessageBox.Show($"Error al inicializar la base de datos:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Initialize fingerprint service
            _fingerprintService = CreateFingerprintService();

            var attendanceService = new AttendanceService(_fingerprintService);
            var mainVM = new MainViewModel(_fingerprintService, attendanceService);

            var mainWindow = new MainWindow { DataContext = mainVM };
            mainWindow.Show();
            mainVM.NavigateToPageCommand.Execute("Dashboard");
        }

        private IFingerprintService CreateFingerprintService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var ftrapPath = Path.Combine(baseDir, "FTRAPI.dll");
            var scanPath = Path.Combine(baseDir, "ftrScanAPI.dll");

            Console.WriteLine();
            Console.WriteLine("── Búsqueda de DLLs Futronic ──");
            Console.WriteLine($"  FTRAPI.dll:     {(File.Exists(ftrapPath) ? "✅ EXISTE" : "❌ NO EXISTE")}");
            Console.WriteLine($"  ftrScanAPI.dll: {(File.Exists(scanPath) ? "✅ EXISTE" : "❌ NO EXISTE")}");

            if (FtrScanApi.IsDllAvailable())
            {
                Console.WriteLine();
                Console.WriteLine("── Intentando abrir dispositivo Futronic ──");

                try
                {
                    var realService = new FutronicService();
                    if (realService.Initialize())
                    {
                        var size = realService.GetImageSize();
                        Console.WriteLine($"✅ Dispositivo FS80H conectado. Imagen: {size.width}x{size.height}");
                        return realService;
                    }
                    else
                    {
                        Console.WriteLine("❌ No se pudo inicializar el dispositivo.");
                        realService.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.GetType().Name} - {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("❌ DLLs de Futronic no encontradas.");
            }

            Console.WriteLine();
            Console.WriteLine("⚠️  Usando servicio de huellas SIMULADO.");
            var simulated = new SimulatedFingerprintService();
            simulated.Initialize();
            return simulated;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _fingerprintService?.Dispose();
            base.OnExit(e);
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}
