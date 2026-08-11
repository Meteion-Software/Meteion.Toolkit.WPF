using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;

[assembly: InternalsVisibleTo("Meteion.Toolkit.WPF.Localization.Tests")]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
[assembly: XmlnsPrefix(@"http://wpf.meteion.ca/winfx/xaml", "mtk")]
[assembly: XmlnsPrefix(@"http://wpf.meteion.ca/winfx/xaml/localization", "lx")]
// We will put the localization converters in a separate namespace to avoid cluttering the main namespace with too many classes.
[assembly: XmlnsDefinition(@"http://wpf.meteion.ca/winfx/xaml/localization", "Meteion.Toolkit.WPF.Localization")]
[assembly: XmlnsDefinition(@"http://wpf.meteion.ca/winfx/xaml/localization", "Meteion.Toolkit.WPF.Localization.Extensions")]
