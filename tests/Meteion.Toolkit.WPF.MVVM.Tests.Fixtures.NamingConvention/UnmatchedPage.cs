using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Fixtures.NamingConvention;

/// <summary>
/// View type with no corresponding view model anywhere in this assembly — should be
/// skipped (logged as a warning) rather than throwing.
/// </summary>
public class UnmatchedPage : Page;
