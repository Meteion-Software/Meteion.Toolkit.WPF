using System.ComponentModel;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;

// Required by INotifyPropertyChanged for the generic constraints these fixtures exist
// to satisfy; never raised since nothing here reacts to property changes.
#pragma warning disable CS0067

public class FakeViewModelA : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
}

public class FakeViewModelB : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
}

#pragma warning restore CS0067
