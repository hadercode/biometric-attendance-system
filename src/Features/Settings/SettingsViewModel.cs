using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LectorHuellas.Core.Data;
using LectorHuellas.Core.Models;

namespace LectorHuellas.Features.Settings
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _selectedProvider = "SQLite";

        [ObservableProperty]
        private string _host = "localhost";

        [ObservableProperty]
        private int _port = 5432;

        [ObservableProperty]
        private string _database = "lector_huellas";

        [ObservableProperty]
        private string _username = "postgres";

        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private string _sqlitePath = "";

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private string _statusColor = "#8B949E";

        [ObservableProperty]
        private bool _showServerFields;

        [ObservableProperty]
        private bool _showSqliteFields = true;

        public string[] Providers { get; } = new[] { "SQLite", "PostgreSQL", "MySQL" };

        public SettingsViewModel()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = DatabaseSettings.Load();
            SelectedProvider = settings.Provider;
            Host = settings.Host;
            Port = settings.Port;
            Database = settings.Database;
            Username = settings.Username;
            Password = settings.Password;
            SqlitePath = settings.GetSqlitePath();
            UpdateFieldVisibility();
            StatusMessage = "Configuración cargada.";
            StatusColor = "#8B949E";
        }

        partial void OnSelectedProviderChanged(string value)
        {
            // Load defaults for the selected provider
            var defaults = DatabaseSettings.GetDefaults(value);
            Host = defaults.Host;
            Port = defaults.Port;
            Database = defaults.Database;
            Username = defaults.Username;
            Password = defaults.Password;
            UpdateFieldVisibility();
        }

        private void UpdateFieldVisibility()
        {
            ShowServerFields = SelectedProvider != "SQLite";
            ShowSqliteFields = SelectedProvider == "SQLite";
        }

        [RelayCommand]
        private async Task TestConnection()
        {
            StatusMessage = "⏳ Probando conexión...";
            StatusColor = "#FDCB6E";

            try
            {
                var settings = BuildSettings();
                using var db = new AppDbContext(settings);
                bool canConnect = await db.Database.CanConnectAsync();

                if (canConnect)
                {
                    StatusMessage = "✅ Conexión exitosa.";
                    StatusColor = "#00B894";
                }
                else
                {
                    StatusMessage = "❌ No se pudo conectar a la base de datos.";
                    StatusColor = "#FF7675";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                StatusColor = "#FF7675";
            }
        }

        [RelayCommand]
        private void SaveSettings()
        {
            try
            {
                var settings = BuildSettings();
                settings.Save();
                StatusMessage = "✅ Configuración guardada. Reinicie la aplicación para aplicar los cambios.";
                StatusColor = "#00B894";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error al guardar: {ex.Message}";
                StatusColor = "#FF7675";
            }
        }

        private DatabaseSettings BuildSettings()
        {
            return new DatabaseSettings
            {
                Provider = SelectedProvider,
                Host = Host,
                Port = Port,
                Database = Database,
                Username = Username,
                Password = Password,
                SqlitePath = SelectedProvider == "SQLite" ? SqlitePath : ""
            };
        }
    }
}
