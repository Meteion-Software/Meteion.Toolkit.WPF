using Meteion.Toolkit.MVVM.Models;
using Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Models;

public class ViewModelViewDictionaryTests
{
    [Fact]
    public void Add_DefaultLifetime_AddsScopedRecord()
    {
        var dict = new ViewModelViewDictionary<Page>();

        dict.Add<FakeViewModelA, FakePageA>();

        var record = dict[typeof(FakeViewModelA)];
        Assert.Equal(typeof(FakePageA), record.PageType);
        Assert.Equal(ServiceLifetime.Scoped, record.Lifetime);
    }

    [Fact]
    public void Add_ExplicitLifetime_IsRespected()
    {
        var dict = new ViewModelViewDictionary<Page>();

        dict.Add<FakeViewModelA, FakePageA>(ServiceLifetime.Singleton);

        Assert.Equal(ServiceLifetime.Singleton, dict[typeof(FakeViewModelA)].Lifetime);
    }
}
