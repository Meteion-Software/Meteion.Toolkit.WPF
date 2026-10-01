using CommunityToolkit.Mvvm.Input;
using Meteion.Toolkit.MVVM.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Meteion.Toolkit.WPF.SampleApp.ViewModels;

/// <summary>
/// Backs the PlaceholderContainer example page: an observable list that can be emptied and refilled (the "no
/// items" case), a selection that can be cleared (the "nothing selected" case), and a flag that forces the
/// placeholder on through <c>ShowPlaceholder</c>.
/// </summary>
public partial class PlaceholderContainerPageViewModel : INotifyPropertyChanged
{
    private readonly INavigationService _navService;
    private string? _selectedOrder;
    private bool _isMaintenance;
    private int _nextOrderNumber = 1;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PlaceholderContainerPageViewModel(INavigationService navService)
    {
        _navService = navService;
        AddOrder();
        AddOrder();
        AddOrder();
    }

    public ObservableCollection<string> Orders { get; } = [];

    public string? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            _selectedOrder = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedOrder)));
        }
    }

    public bool IsMaintenance
    {
        get => _isMaintenance;
        set
        {
            _isMaintenance = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMaintenance)));
        }
    }

    [RelayCommand]
    public void AddOrder() => Orders.Add($"Order #{_nextOrderNumber++}");

    [RelayCommand]
    public void ClearOrders()
    {
        Orders.Clear();
        SelectedOrder = null;
    }

    [RelayCommand]
    public void ClearSelection() => SelectedOrder = null;

    [RelayCommand]
    public Task GoBack() => _navService.GoBack();
}
