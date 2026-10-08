using Meteion.Toolkit.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Fixtures.NamingConvention;

// Required by INotifyPropertyChanged; this fixture exists purely to be discovered by
// naming convention, never to actually raise property-change notifications.
#pragma warning disable CS0067
[ViewModelOptions(Lifetime = ServiceLifetime.Singleton)]
public class SingletonOptionsPageViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
}
#pragma warning restore CS0067
