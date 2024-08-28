using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CommandRunner.ViewModels;

namespace CommandRunner.Helpers
{
    public class CommandStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CommandState state)
            {
                switch (state)
                {
                    case CommandState.Running:
                        return Brushes.Yellow;
                    case CommandState.Completed:
                        return Brushes.Green;
                    case CommandState.Error:
                        return Brushes.Red;
                    default:
                        return Brushes.Transparent;
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}