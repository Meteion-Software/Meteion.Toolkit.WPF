using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.Localization.Abstractions;

public interface ILocalizationService : INotifyPropertyChanged
{
    string GetString(string key, Assembly? resourceAssembly = null);
    CultureInfo CurrentCulture { get; set; }
    event EventHandler<CultureChangedEventArgs> CultureChanged;
}

public class CultureChangedEventArgs(CultureInfo culture) : EventArgs
{
    public CultureInfo Culture { get; } = culture;
}