using System.Windows;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.Controls;

/// <summary>
/// An overlay that dims its area and shows an animated spinner while <see cref="IsBusy"/> is true.
/// Place it in the same panel cell as the content it should cover (e.g. alongside a navigation Frame)
/// so it renders on top of that content.
/// </summary>
public class BusyOverlay : Control
{
    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(BusyOverlay), new PropertyMetadata(false));

    static BusyOverlay()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyOverlay), new FrameworkPropertyMetadata(typeof(BusyOverlay)));
    }

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }
}
