using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Pulsebar.Windows;

namespace Pulsebar.Converters
{
    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string _format = (string)parameter;

            if (string.IsNullOrEmpty(_format))
            {
                return value.ToString();
            }
            else
            {
                return string.Format(culture, _format, value);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int _return = 0;

            int.TryParse(value.ToString(), out _return);

            return _return;
        }
    }

    public class HotkeyToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Hotkey _hotkey = (Hotkey)value;

            if (_hotkey == null)
            {
                return "None";
            }

            return
                (_hotkey.AltMod ? "Alt + " : "") +
                (_hotkey.CtrlMod ? "Ctrl + " : "") +
                (_hotkey.ShiftMod ? "Shift + " : "") +
                (_hotkey.WinMod ? "Win + " : "") +
                new KeyConverter().ConvertToString(_hotkey.WinKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double _value = (double)value;

            return string.Format("{0:0}%", _value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class BoolInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    public class MetricLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string _value = (string)value;

            return string.Format("{0}:", _value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class LoadSeverityColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush _low = MakeBrush("#3E8F4C");
        private static readonly SolidColorBrush _medium = MakeBrush("#B4791E");
        private static readonly SolidColorBrush _high = MakeBrush("#B23A2E");

        private static SolidColorBrush MakeBrush(string hex)
        {
            SolidColorBrush _brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            _brush.Freeze();
            return _brush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double _value = value is double ? (double)value : 0d;

            if (_value >= 85d)
            {
                return _high;
            }

            if (_value >= 60d)
            {
                return _medium;
            }

            return _low;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class DriveSeverityColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush _ok = MakeBrush("#3E8F4C");
        private static readonly SolidColorBrush _low = MakeBrush("#B4791E");
        private static readonly SolidColorBrush _critical = MakeBrush("#B23A2E");

        private static SolidColorBrush MakeBrush(string hex)
        {
            SolidColorBrush _brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            _brush.Freeze();
            return _brush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double _usedPercent = value is double ? (double)value : 0d;

            if (_usedPercent >= 95d)
            {
                return _critical;
            }

            if (_usedPercent >= 90d)
            {
                return _low;
            }

            return _ok;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FontToSpaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int _value = (int)value;

            return new Thickness(0, 0, _value * 0.4d, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
