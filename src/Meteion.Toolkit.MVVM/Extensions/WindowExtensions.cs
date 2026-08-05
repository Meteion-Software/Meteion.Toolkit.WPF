using System.Windows;
using System.Windows.Controls;
using Meteion.Toolkit.WPF;

namespace Meteion.Toolkit.MVVM
{
    public static class WindowExtensions
    {
        /// <summary>
        /// Retrieve the data context from the frame contained within the window. If the window's content is not a frame, this method returns null.
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static object? GetDataContext(this Window window)
        {
            if (window.Content is Frame frame)
            {
                return frame.GetDataContext();
            }

            return null;
        }
    }
}
