using Meteion.Toolkit.Localization.Abstractions;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Tests.Fakes;

/// <summary>
/// Fake ILocalizationService with a controllable GetString result and a real,
/// raisable CultureChanged event — WeakEventManager hooks the real event accessors
/// on ILocalizationService, so this needs to be a genuine implementation, not a mock
/// that only tracks calls.
/// </summary>
public sealed class FakeLocalizationService : ILocalizationService
{
    public string? ValueToReturn { get; set; }
    public string? LastRequestedKey { get; private set; }
    public Assembly? LastRequestedAssembly { get; private set; }
    public int GetStringCallCount { get; private set; }

    public CultureInfo CurrentCulture { get; set; } = CultureInfo.InvariantCulture;

    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    // Required by ILocalizationService; this fake never raises it since nothing here
    // exercises it — CultureChanged is the one WeakEventManager/proxy tests care about.
#pragma warning disable CS0067
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    public string GetString(string key, Assembly? resourceAssembly = null)
    {
        LastRequestedKey = key;
        LastRequestedAssembly = resourceAssembly;
        GetStringCallCount++;
        return ValueToReturn ?? key;
    }

    public void RaiseCultureChanged(CultureInfo culture)
        => CultureChanged?.Invoke(this, new CultureChangedEventArgs(culture));
}
