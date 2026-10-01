using CommunityToolkit.Mvvm.Input;
using Meteion.Toolkit.MVVM.Services;
using System.ComponentModel;

namespace Meteion.Toolkit.WPF.SampleApp.ViewModels;

/// <summary>
/// A single DataGrid row: a plain, non-localized name plus just the short suffix of a resource
/// key, which <c>lx:LocalizedBinding</c> combines with a shared prefix to localize the cell.
/// </summary>
public sealed record FeatureRow(string Name, string FeatureSuffix);

/// <summary>
/// Backs the LocalizedBinding example page - a DataGrid column, which is the case
/// <c>lx:LocalizedValue</c> can't handle because <c>DataGridTextColumn.Binding</c> is a plain CLR
/// property rather than a DependencyProperty.
/// </summary>
public partial class LocalizedBindingPageViewModel : INotifyPropertyChanged
{
    private readonly INavigationService _navService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<FeatureRow> Rows { get; } =
    [
        new("First row", "Alpha"),
        new("Second row", "Beta"),
        new("Third row", "Gamma"),
    ];

    public LocalizedBindingPageViewModel(INavigationService navService)
    {
        _navService = navService;
    }

    [RelayCommand]
    public Task GoBack() => _navService.GoBack();
}
