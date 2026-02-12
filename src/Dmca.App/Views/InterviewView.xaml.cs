using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Dmca.App.Views;

public partial class InterviewView : UserControl
{
    public InterviewView()
    {
        InitializeComponent();
    }
}

/// <summary>
/// Converts CurrentStep == ConverterParameter to Visibility.
/// </summary>
internal sealed class StepVisibilityConverter : IValueConverter
{
    public static readonly StepVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int step && parameter is string paramStr && int.TryParse(paramStr, out var target))
            return step == target ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
