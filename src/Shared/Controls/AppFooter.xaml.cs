using System;
using System.Windows;
using System.Windows.Controls;

namespace LectorHuellas.Shared.Controls
{
    public partial class AppFooter : UserControl
    {
        public static readonly DependencyProperty CurrentYearProperty =
            DependencyProperty.Register("CurrentYear", typeof(string), typeof(AppFooter), new PropertyMetadata(null));

        public string CurrentYear
        {
            get => (string)GetValue(CurrentYearProperty);
            set => SetValue(CurrentYearProperty, value);
        }

        public AppFooter()
        {
            InitializeComponent();
            CurrentYear = DateTime.Now.Year.ToString();
        }

        private void Documentation_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            bool isAdmin = window is Features.Main.AdminWindow;
            int roleId = 0;

            if (window?.DataContext is Features.Main.MainViewModel vm)
            {
                roleId = vm.CurrentUserRoleId;
            }

            var docWindow = new DocumentationWindow(isAdmin, roleId);
            docWindow.Owner = window;
            docWindow.ShowDialog();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow();
            about.Owner = Window.GetWindow(this);
            about.ShowDialog();
        }
    }
}
