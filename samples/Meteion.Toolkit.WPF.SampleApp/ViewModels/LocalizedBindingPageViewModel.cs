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

    /// <summary>A non-localized value fed to the format example as <c>{0}</c>.</summary>
    public string OwnerName { get; } = "Ada";

    /// <summary>A number fed to the format example as <c>{1:N0}</c>; groups digits per culture.</summary>
    public int ItemCount { get; } = 1234567;

    /// <summary>An amount fed to the composite <c>CultureAwareFormat</c> example.</summary>
    public decimal Spent { get; } = 1234.5m;

    /// <summary>The budget <see cref="Spent"/> is compared against.</summary>
    public decimal Budget { get; } = 5000m;

    public LocalizedBindingPageViewModel(INavigationService navService)
    {
        _navService = navService;
    }

    [RelayCommand]
    public Task GoBack() => _navService.GoBack();
}
