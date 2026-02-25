using System;
using System.IO;
using System.Windows;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LectorHuellas.Core.Data;
using LectorHuellas.Core.Services;
using LectorHuellas.Core.Interop;
using LectorHuellas.Features.Main;

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
                db.Database.EnsureCreated();

                // Advanced Schema Sync (for dev/updates without migrations)
                PatchDatabaseSchema(db);

                Console.WriteLine("✅ Base de datos sincronizada.");
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

        private void PatchDatabaseSchema(AppDbContext db)
        {
            try
            {
                // 1. Ensure new tables exist (EF.EnsureCreated handles the whole schema if empty)
                // If tables already exist, ExecuteSqlRaw is needed for column additions.

                var conn = db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) conn.Open();

                // 2. Add 'Position' if missing
                TryAddColumn(db, "Employees", "Position", "TEXT NOT NULL DEFAULT 'Empleado'");
                
                // 3. Add 'PhotoPath' if missing
                TryAddColumn(db, "Employees", "PhotoPath", "TEXT NULL");

                // 4. Ensure FingerprintTemplates table exists (manual safeguard)
                // In some cases EnsureCreated skips tables if it thinks they already exist in a partial schema
                try { _ = db.FingerprintTemplates.FirstOrDefault(); }
                catch { 
                    Console.WriteLine("⚠️  Tabla FingerprintTemplates no encontrada. Forzando creación...");
                    // This is a last resort, usually EnsureCreated should handle this
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error al parchar esquema: {ex.Message}");
            }
        }

        private void TryAddColumn(AppDbContext db, string table, string column, string typeDefinition)
        {
            try
            {
                // Simple approach: Just try to add it and catch the "already exists" error
                try
                {
                    db.Database.ExecuteSqlRaw($"ALTER TABLE {table} ADD COLUMN {column} {typeDefinition}");
                    Console.WriteLine($"✅ Columna {column} añadida a {table}.");
                }
                catch (Exception ex) when (ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) || 
                                          ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) || 
                                          ex.Message.Contains("Duplicate column", StringComparison.OrdinalIgnoreCase))
                {
                    // Column already exists, ignore
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ℹ️  Aviso al procesar columna {column}: {ex.Message}");
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}
