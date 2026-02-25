using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LectorHuellas.Shared.Converters
{
    /// <summary>
    /// Converts raw grayscale fingerprint byte[] to a WPF BitmapSource for display.
    /// </summary>
    public class FingerprintImageConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return null;
            if (values[0] is not byte[] imageData) return null;
            if (values[1] is not int width || values[2] is not int height) return null;
            if (width <= 0 || height <= 0) return null;

            return CreateBitmapFromGrayscale(imageData, width, height);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static BitmapSource? CreateBitmapFromGrayscale(byte[] grayscaleData, int width, int height)
        {
            try
            {
                if (grayscaleData.Length < width * height) return null;

                // Convert grayscale to BGRA32 for WPF
                var stride = width * 4;
                var pixels = new byte[stride * height];

                for (int i = 0; i < width * height; i++)
                {
                    var gray = grayscaleData[i];
                    var offset = i * 4;
                    pixels[offset] = gray;     // B
                    pixels[offset + 1] = gray; // G
                    pixels[offset + 2] = gray; // R
                    pixels[offset + 3] = 255;  // A
                }

                return BitmapSource.Create(
                    width, height, 96, 96,
                    PixelFormats.Bgra32, null,
                    pixels, stride);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Boolean to Visibility converter
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }

    /// <summary>
    /// Inverted Boolean to Visibility converter
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Converts a string to true if it matches the ConverterParameter (for RadioButton IsChecked)
    /// </summary>
    public class StringMatchConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return parameter?.ToString() ?? "";
            return System.Windows.Data.Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converts a page name string to Visibility based on parameter match
    /// </summary>
    public class PageVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;

            string valStr = value.ToString() ?? string.Empty;
            string paramStr = parameter.ToString() ?? string.Empty;

            return string.Equals(valStr, paramStr, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Shows Visible when value is null, Collapsed otherwise (for placeholder icons)
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Shows Visible when string is not empty
    /// </summary>
    public class NotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Shows ✅ if value is non-null, ❌ otherwise (for template column)
    /// </summary>
    public class HasValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? "✅" : "❌";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverts a boolean value - usable as a MarkupExtension
    /// </summary>
    public class InverseBoolConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return false;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }

    /// <summary>
    /// Converts boolean to one of two strings (parameter format: "TrueString|FalseString")
    /// </summary>
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parts = parameter?.ToString()?.Split('|') ?? new[] { "True", "False" };
            if (parts.Length < 2) return value?.ToString() ?? "";
            
            return (value is bool b && b) ? parts[0] : parts[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
