using Meteion.Toolkit.MVVM.Models;
using Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;
using Meteion.Toolkit.WPF.MVVM.Tests.Fixtures.NamingConvention;
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
}
