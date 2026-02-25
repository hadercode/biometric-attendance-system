using System;
using System.IO;
using System.Text.Json;

namespace LectorHuellas.Models
{
    public class DatabaseSettings
    {
        public string Provider { get; set; } = "SQLite";  // SQLite | PostgreSQL | MySQL
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = "lector_huellas";
        public string Username { get; set; } = "postgres";
        public string Password { get; set; } = "";
        public string SqlitePath { get; set; } = "";

        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LectorHuellas");

        private static readonly string SettingsFile = Path.Combine(SettingsDir, "dbsettings.json");

        public static DatabaseSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize<DatabaseSettings>(json) ?? new DatabaseSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading DB settings: {ex.Message}");
            }
            return new DatabaseSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
                Console.WriteLine($"DB Settings saved to {SettingsFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving DB settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Build connection string based on the selected provider.
        /// </summary>
        public string GetConnectionString()
        {
            return Provider switch
            {
                "PostgreSQL" => $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}",
                "MySQL" => $"Server={Host};Port={Port};Database={Database};User={Username};Password={Password};",
                _ => $"Data Source={GetSqlitePath()}" // SQLite
            };
        }

        public string GetSqlitePath()
        {
            if (!string.IsNullOrWhiteSpace(SqlitePath))
                return SqlitePath;

            return Path.Combine(SettingsDir, "attendance.db");
        }

        /// <summary>
        /// Get default settings for each provider.
        /// </summary>
        public static DatabaseSettings GetDefaults(string provider)
        {
            return provider switch
            {
                "PostgreSQL" => new DatabaseSettings
                {
                    Provider = "PostgreSQL",
                    Host = "localhost",
                    Port = 5432,
                    Database = "lector_huellas",
                    Username = "postgres",
                    Password = ""
                },
                "MySQL" => new DatabaseSettings
                {
                    Provider = "MySQL",
                    Host = "localhost",
                    Port = 3306,
                    Database = "lector_huellas",
                    Username = "root",
                    Password = ""
                },
                _ => new DatabaseSettings
                {
                    Provider = "SQLite",
                    Host = "localhost",
                    Port = 0,
                    Database = "",
                    Username = "",
                    Password = ""
                }
            };
        }
    }
}
