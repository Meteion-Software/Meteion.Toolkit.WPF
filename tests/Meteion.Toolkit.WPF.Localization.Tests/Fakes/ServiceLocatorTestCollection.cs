namespace Meteion.Toolkit.WPF.Localization.Tests.Fakes;

/// <summary>
/// LocalizationServiceLocator.ServiceProviderAccessor is static, process-wide mutable
/// state. Any test class that overrides it must run in this collection so xUnit
/// executes them sequentially — otherwise two test classes racing to swap the
/// accessor in parallel would corrupt each other's fakes.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class ServiceLocatorTestCollection
{
    public const string Name = "LocalizationServiceLocator";
}
