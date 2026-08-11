using System.Windows;

namespace Meteion.Toolkit.WPF.Hosting.Tests.Fixtures;

/// <summary>
/// These are never actually constructed in these tests — only registered and checked
/// via DI metadata — so declaring them doesn't carry any STA requirement.
/// </summary>
public class DummyWindow : Window;

public class DummyApp : WpfGenericHostApplication
{
    public override void PerformInitializeComponent() { }
}
