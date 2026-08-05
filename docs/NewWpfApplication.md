# New WPF Application

Add:
```csproj
	<EnableDefaultApplicationDefinition>false</EnableDefaultApplicationDefinition>
```

To your project file to allow using Program.cs.

Create a basic Program.cs:
```cs
public static class Program
{

    [STAThread]
    public static void Main(string[] args)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new Exception("Main application thread is not STA, but many components require this.");
        }

        var host = new ToolkitHostBuilder()
            .ConfigureLaunchWindow<MainWindow>()
            .ConfigureApplication<App>()
            .Build();
    }
}
```

Modify App.xaml.cs to inherit from ``WpfGenericHostApplication``:
```cs
    public partial class App : WpfGenericHostApplication
    {
        public override void PerformInitializeComponent()
        {
            InitializeComponent();
        }
    }
```

It is important you override to call InitializeComponent. 

Next modify App.xaml:
```xaml
<meteion:WpfGenericHostApplication x:Class="Meteion.Toolkit.WPF.SampleApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:Meteion.Toolkit.WPF.SampleApp"
             xmlns:meteion="http://wpf.meteion.ca/winfx/xaml">
    <meteion:WpfGenericHostApplication.Resources>
         
    </meteion:WpfGenericHostApplication.Resources>
</meteion:WpfGenericHostApplication>

```

Important: Ensure you remove StartupUri, as this is no longer used!