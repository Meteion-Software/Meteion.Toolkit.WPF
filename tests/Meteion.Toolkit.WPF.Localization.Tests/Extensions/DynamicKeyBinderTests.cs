using Meteion.Toolkit.WPF.Localization.Extensions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Tests.Extensions;

public class DynamicKeyBinderTests
{
    private static readonly System.Reflection.Assembly SomeAssembly = typeof(DynamicKeyBinderTests).Assembly;

    [StaFact]
    public void Bind_InitialSourceValue_ForwardsKeyToProxyImmediately()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new DynamicToolkitLocalizationProxy(service, SomeAssembly);
        var target = new TextBlock { DataContext = new KeySource { TitleKey = "Greeting" } };

        DynamicKeyBinder.Bind(target, proxy, new Binding(nameof(KeySource.TitleKey)));

        Assert.Equal("Greeting", proxy.Key);
        Assert.Equal("Hello", proxy.Value);
    }

    [StaFact]
    public void Bind_SourcePropertyChanges_UpdatesProxyKey()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new DynamicToolkitLocalizationProxy(service, SomeAssembly);
        var source = new KeySource { TitleKey = "Greeting" };
        var target = new TextBlock { DataContext = source };
        DynamicKeyBinder.Bind(target, proxy, new Binding(nameof(KeySource.TitleKey)));

        service.ValueToReturn = "Goodbye";
        source.TitleKey = "Farewell";

        Assert.Equal("Farewell", proxy.Key);
        Assert.Equal("Goodbye", proxy.Value);
    }

    private sealed class KeySource : INotifyPropertyChanged
    {
        private string? _titleKey;

        public string? TitleKey
        {
            get => _titleKey;
            set
            {
                _titleKey = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitleKey)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
