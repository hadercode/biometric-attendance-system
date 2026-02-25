using System;
using System.Linq;
using System.Windows;
using LectorHuellas.Core.Models;

namespace LectorHuellas.Core.Services
{
    public interface IThemeService
    {
        bool IsDarkTheme { get; }
        void ToggleTheme();
        void SetTheme(bool isDark);
        void Initialize();
    }

    public class ThemeService : IThemeService
    {
        public bool IsDarkTheme { get; private set; } = true;

        public void Initialize()
        {
            var settings = DatabaseSettings.Load();
            SetTheme(settings.Theme != "Light");
        }

        public void ToggleTheme()
        {
            SetTheme(!IsDarkTheme);
            
            // Persist setting
            var settings = DatabaseSettings.Load();
            settings.Theme = IsDarkTheme ? "Dark" : "Light";
            settings.Save();
        }

        public void SetTheme(bool isDark)
        {
            IsDarkTheme = isDark;
            var themeUri = isDark 
                ? new Uri("Shared/Resources/DarkTheme.xaml", UriKind.Relative)
                : new Uri("Shared/Resources/LightTheme.xaml", UriKind.Relative);

            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var oldTheme = dictionaries.FirstOrDefault(d => 
                d.Source != null && (d.Source.OriginalString.Contains("DarkTheme.xaml") || 
                                     d.Source.OriginalString.Contains("LightTheme.xaml")));

            if (oldTheme != null)
            {
                int index = dictionaries.IndexOf(oldTheme);
                dictionaries[index] = new ResourceDictionary { Source = themeUri };
            }
            else
            {
                dictionaries.Insert(0, new ResourceDictionary { Source = themeUri });
            }
        }
    }
}
