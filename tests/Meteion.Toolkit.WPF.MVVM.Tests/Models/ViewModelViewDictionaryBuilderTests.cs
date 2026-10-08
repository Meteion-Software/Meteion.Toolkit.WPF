using Meteion.Toolkit.MVVM.Models;
using Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;
using Meteion.Toolkit.WPF.MVVM.Tests.Fixtures.NamingConvention;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Models;

public class ViewModelViewDictionaryBuilderTests
{
    [Fact]
    public void Add_AddsToUnderlyingDictionary()
    {
        var builder = new ViewModelViewDictionaryBuilder<Page>();

        builder.Add<FakeViewModelA, FakePageA>();
        var dict = builder.Build();

        Assert.Equal(typeof(FakePageA), dict[typeof(FakeViewModelA)].PageType);
    }

    [Fact]
    public void AddFromAssembly_MatchesBaseNamingConvention()
    {
        var builder = new ViewModelViewDictionaryBuilder<Page>();

        builder.AddFromAssembly(typeof(FirstMatchPage).Assembly);
        var dict = builder.Build();

        Assert.Equal(typeof(FirstMatchPage), dict[typeof(FirstMatchPageViewModel)].PageType);
    }

    [Fact]
    public void AddFromAssembly_MatchesPageSpecificNamingConvention()
    {
        var builder = new ViewModelViewDictionaryBuilder<Page>();

        builder.AddFromAssembly(typeof(SecondMatch).Assembly);
        var dict = builder.Build();

        Assert.Equal(typeof(SecondMatch), dict[typeof(SecondMatchPageViewModel)].PageType);
    }

    [Fact]
    public void AddFromAssembly_ViewWithNoMatchingViewModel_IsSkippedWithoutThrowing()
    {
        var builder = new ViewModelViewDictionaryBuilder<Page>();

        builder.AddFromAssembly(typeof(UnmatchedPage).Assembly);
        var dict = builder.Build();

        Assert.DoesNotContain(dict.Values, r => r.PageType == typeof(UnmatchedPage));
    }

    [Fact]
    public void AddFromAssembly_ViewModelWithOptionsAttribute_UsesAttributeLifetime()
    {
        var builder = new ViewModelViewDictionaryBuilder<Page>();

        builder.AddFromAssembly(typeof(SingletonOptionsPage).Assembly);
        var dict = builder.Build();

        Assert.Equal(ServiceLifetime.Singleton, dict[typeof(SingletonOptionsPageViewModel)].Lifetime);
    }

    [Fact]
    public void AddFromAssembly_ViewModelWithoutOptionsAttribute_DefaultsToScoped()
    {
        var builder = new ViewModelViewDictionaryBuilder<Page>();

        builder.AddFromAssembly(typeof(FirstMatchPage).Assembly);
        var dict = builder.Build();

        Assert.Equal(ServiceLifetime.Scoped, dict[typeof(FirstMatchPageViewModel)].Lifetime);
    }

    [Fact]
    public void Add_ExplicitLifetime_OverridesOptionsAttribute()
    {
        var builder = new ViewModelViewDictionaryBuilder<Page>();

        builder.Add<SingletonOptionsPageViewModel, FakePageA>(ServiceLifetime.Transient);
        var dict = builder.Build();

        Assert.Equal(ServiceLifetime.Transient, dict[typeof(SingletonOptionsPageViewModel)].Lifetime);
    }

    [Fact]
    public void Add_NoExplicitLifetime_UsesOptionsAttribute()
    {
        var builder = new ViewModelViewDictionaryBuilder<Page>();

        builder.Add<SingletonOptionsPageViewModel, FakePageA>();
        var dict = builder.Build();

        Assert.Equal(ServiceLifetime.Singleton, dict[typeof(SingletonOptionsPageViewModel)].Lifetime);
    }
}
