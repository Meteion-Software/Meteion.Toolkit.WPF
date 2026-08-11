using Meteion.Toolkit.Localization.Abstractions;

namespace Meteion.Toolkit.WPF.Localization.Tests.Abstractions;

public class ExceptionTests
{
    [Fact]
    public void LocalizationConfigurationException_PreservesMessage()
    {
        var ex = new LocalizationConfigurationException("no assembly configured");

        Assert.Equal("no assembly configured", ex.Message);
    }

    [Fact]
    public void LocalizationKeyNotFoundException_ExposesKeyAndAssemblyAsProperties()
    {
        var assembly = typeof(ExceptionTests).Assembly;

        var ex = new LocalizationKeyNotFoundException("Greeting", assembly);

        Assert.Equal("Greeting", ex.Key);
        Assert.Same(assembly, ex.ResourceAssembly);
        Assert.Contains("Greeting", ex.Message);
        Assert.Contains(assembly.GetName().Name!, ex.Message);
    }
}
