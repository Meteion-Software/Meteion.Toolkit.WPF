using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Fixtures.NamingConvention;

/// <summary>
/// View type whose name already ends in "Page" — should be matched by the base
/// "{ViewName}ViewModel" naming rule alone ("FirstMatchPageViewModel"), without
/// needing the Page-specific "{ViewName}PageViewModel" rule.
/// </summary>
public class FirstMatchPage : Page;
