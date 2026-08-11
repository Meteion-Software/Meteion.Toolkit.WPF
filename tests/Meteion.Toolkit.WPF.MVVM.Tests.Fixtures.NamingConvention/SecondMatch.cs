using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Fixtures.NamingConvention;

/// <summary>
/// View type whose name does NOT end in "Page" — the base "{ViewName}ViewModel" rule
/// would look for "SecondMatchViewModel" (which doesn't exist). Only matched via the
/// Page-specific "{ViewName}PageViewModel" rule ("SecondMatchPageViewModel").
/// </summary>
public class SecondMatch : Page;
