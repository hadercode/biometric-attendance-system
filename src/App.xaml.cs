using System;
using System.IO;
using System.Windows;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LectorHuellas.Core.Data;
using LectorHuellas.Core.Services;
using LectorHuellas.Core.Interop;
using LectorHuellas.Features.Main;
using LectorHuellas.Features.Attendance;
using LectorHuellas.Features.Auth;

namespace LectorHuellas
{
    public partial class App : Application
    {
        private AttendanceWindow? _attendanceWindow;
        private LoginWindow? _loginWindow;
        private AdminWindow? _adminWindow;
        private IFingerprintService _fingerprintService = null!;
        private IEmployeeService _employeeService = null!;
        private ICommonService _commonService = null!;
        private AttendanceService _attendanceService = null!;
        private MainViewModel? _mainVM;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global exception handling
            DispatcherUnhandledException += (s, args) =>
            {
                Console.WriteLine($"\a❌ ERROR FATAL (Dispatcher): {args.Exception.Message}");
                Console.WriteLine(args.Exception.StackTrace);
                MessageBox.Show($"Ocurrió un error inesperado:\n{args.Exception.Message}", 
                    "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
                Shutdown();
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Console.WriteLine($"\a❌ ERROR FATAL (AppDomain): {ex?.Message}");
                MessageBox.Show($"Error crítico del sistema:\n{ex?.Message}", 
                    "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            AllocConsole();
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine("  LectorHuellas - Control de Asistencia");
            Console.WriteLine("═══════════════════════════════════════════");

            try
            {
                Console.WriteLine("⏳ Inicializando base de datos...");
                using var db = new AppDbContext();
                // EnsureCreated() only creates if the DB doesn't exist. 
                // For MySQL/Postgres we usually need manual schema patching if tables were added.
                db.Database.EnsureCreated();
                PatchDatabaseSchema(db);
                Console.WriteLine("✅ Base de datos lista.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error DB: {ex.Message}");
                MessageBox.Show($"Error al inicializar la base de datos:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            _fingerprintService = CreateFingerprintService();
            _employeeService = new EmployeeService();
            _commonService = new CommonService();
            
            _attendanceService = new AttendanceService(_fingerprintService, _employeeService);
            _mainVM = new MainViewModel(_fingerprintService, _employeeService, _commonService, _attendanceService);
            
            // ... rest of the setup ...
            // Handle admin navigation
            _mainVM.AttendanceVM.AdminAccessRequested += (s, ev) => 
            {
                _mainVM.AttendanceVM.StopScanning();
                _loginWindow = new LoginWindow { DataContext = _mainVM };
                _loginWindow.ShowDialog();
                if (!_mainVM.IsAdminMode) _mainVM.AttendanceVM.StartScanning();
            };
            
            _mainVM.LoginVM.LoginSuccess += (s, ev) => 
            {
                _loginWindow?.Close();
                _attendanceWindow?.Hide();
                _adminWindow = new AdminWindow { DataContext = _mainVM };
                _adminWindow.Closed += (s2, ev2) => 
                {
                    if (_mainVM.IsPublicMode) _attendanceWindow?.Show();
                    else Shutdown();
                };
                _adminWindow.Show();
                _mainVM.NavigateToPageCommand.Execute("Dashboard");
            };

            _mainVM.LoginVM.BackRequested += (s, ev) => _loginWindow?.Close();

            _mainVM.PropertyChanged += (s, ev) => 
            {
                if (ev.PropertyName == nameof(MainViewModel.IsPublicMode) && _mainVM.IsPublicMode)
                {
                    _adminWindow?.Close();
                    _attendanceWindow?.Show();
                    _mainVM.AttendanceVM.StartScanning();
                }
            };

            _attendanceWindow = new AttendanceWindow { DataContext = _mainVM };
            _attendanceWindow.Show();
            
            Console.WriteLine("🚀 Sistema iniciado en Modo Asistencia.");
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
            _mainVM?.Dispose();
            _fingerprintService?.Dispose();
            base.OnExit(e);
        }

        private void PatchDatabaseSchema(AppDbContext db)
        {
            try
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) conn.Open();

                // Ensure support tables exist for the biometric system
                string createAttendanceSql = @"
                    CREATE TABLE IF NOT EXISTS attendance_records (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        employee_id INT NOT NULL,
                        timestamp DATETIME NOT NULL,
                        type VARCHAR(50) NOT NULL,
                        FOREIGN KEY (employee_id) REFERENCES employees(id) ON DELETE CASCADE
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                
                string createFingerprintsSql = @"
                    CREATE TABLE IF NOT EXISTS fingerprint_templates (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        employee_id INT NOT NULL,
                        finger_type VARCHAR(50) NOT NULL,
                        template_data LONGBLOB NOT NULL,
                        captured_at DATETIME NOT NULL,
                        FOREIGN KEY (employee_id) REFERENCES employees(id) ON DELETE CASCADE,
                        UNIQUE KEY uk_employee_finger (employee_id, finger_type)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                string createDepartmentsSql = @"
                    CREATE TABLE IF NOT EXISTS departamento (
                        codigo VARCHAR(3) PRIMARY KEY,
                        dpto VARCHAR(100) NOT NULL
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                string createUnitsSql = @"
                    CREATE TABLE IF NOT EXISTS unidad (
                        codigo VARCHAR(3) PRIMARY KEY,
                        unidad VARCHAR(100) NOT NULL
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                string createManagementsSql = @"
                    CREATE TABLE IF NOT EXISTS gerencia (
                        codigo VARCHAR(3) PRIMARY KEY,
                        gerencia VARCHAR(100) NOT NULL
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                string createShiftsSql = @"
                    CREATE TABLE IF NOT EXISTS turno (
                        codigo VARCHAR(3) PRIMARY KEY,
                        des VARCHAR(100) NOT NULL,
                        limite VARCHAR(50),
                        amanecer VARCHAR(50),
                        tarde VARCHAR(50),
                        sobre VARCHAR(50),
                        holgura VARCHAR(50),
                        descanso VARCHAR(50),
                        duracion VARCHAR(50),
                        horario VARCHAR(50),
                        entrada VARCHAR(50)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                db.Database.ExecuteSqlRaw(createAttendanceSql);
                db.Database.ExecuteSqlRaw(createFingerprintsSql);
                db.Database.ExecuteSqlRaw(createDepartmentsSql);
                db.Database.ExecuteSqlRaw(createUnitsSql);
                db.Database.ExecuteSqlRaw(createManagementsSql);
                db.Database.ExecuteSqlRaw(createShiftsSql);

                // Aggressive type conversion: If tables already existed with VARCHAR employee_id, convert them to INT
                // This is needed because 'CREATE TABLE IF NOT EXISTS' doesn't update existing table structures.
                try { db.Database.ExecuteSqlRaw("ALTER TABLE attendance_records MODIFY COLUMN employee_id INT NOT NULL;"); } catch { }
                try { db.Database.ExecuteSqlRaw("ALTER TABLE fingerprint_templates MODIFY COLUMN employee_id INT NOT NULL;"); } catch { }
                
                Console.WriteLine("✅ Tablas de sistema (asistencia/huellas) verificadas y actualizadas.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error al parchar esquema: {ex.Message}");
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
