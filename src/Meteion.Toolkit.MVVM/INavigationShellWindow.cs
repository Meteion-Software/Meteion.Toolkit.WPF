using System.Windows.Controls;

namespace Meteion.Toolkit.MVVM;

public interface INavigationShellWindow
{
    Frame GetNavigationFrame();

    void Show();

    void Close();
}
