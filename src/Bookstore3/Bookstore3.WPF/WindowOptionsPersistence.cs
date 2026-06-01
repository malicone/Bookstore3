using Bookstore3.Repository;
using System.Windows;

namespace Bookstore3.WPF;

internal static class WindowOptionsPersistence
{
    public static bool Save(IAppOptionRepository repository, Window window, Func<string, string> getFullOptionName)
    {
        if (window.WindowState == WindowState.Maximized)
        {
            return repository.SetOptionAsString(
                getFullOptionName("WindowState"),
                WindowState.Maximized.ToString());
        }

        var result = repository.SetOptionAsString(
            getFullOptionName("WindowState"),
            WindowState.Normal.ToString());
        if (repository.SetOptionAsDouble(getFullOptionName("WindowLeft"), window.Left) == false)
            result = false;
        if (repository.SetOptionAsDouble(getFullOptionName("WindowTop"), window.Top) == false)
            result = false;
        if (repository.SetOptionAsDouble(getFullOptionName("WindowWidth"), window.Width) == false)
            result = false;
        if (repository.SetOptionAsDouble(getFullOptionName("WindowHeight"), window.Height) == false)
            result = false;
        return result;
    }

    public static bool TryApply(
        IAppOptionRepository repository,
        Window window,
        Func<string, string> getFullOptionName,
        double minWidth,
        double minHeight)
    {
        var windowStateStr = repository.GetOptionAsString(getFullOptionName("WindowState"));
        if (string.IsNullOrEmpty(windowStateStr) ||
            Enum.TryParse(windowStateStr, out WindowState windowState) == false)
            return false;

        if (windowState == WindowState.Maximized)
        {
            window.WindowState = WindowState.Maximized;
            return true;
        }

        if (windowState != WindowState.Normal)
            return false;

        var left = repository.GetOptionAsDouble(getFullOptionName("WindowLeft"));
        var top = repository.GetOptionAsDouble(getFullOptionName("WindowTop"));
        var width = repository.GetOptionAsDouble(getFullOptionName("WindowWidth"));
        var height = repository.GetOptionAsDouble(getFullOptionName("WindowHeight"));
        if (left is null || top is null || width is null || height is null)
            return false;

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Width = Math.Max(minWidth, width.Value);
        window.Height = Math.Max(minHeight, height.Value);
        window.Left = left.Value;
        window.Top = top.Value;
        ClampToWorkArea(window);
        window.WindowState = WindowState.Normal;
        return true;
    }

    private static void ClampToWorkArea(Window window)
    {
        var area = SystemParameters.WorkArea;
        if (window.Width > area.Width)
            window.Width = area.Width;
        if (window.Height > area.Height)
            window.Height = area.Height;
        if (window.Left < area.Left)
            window.Left = area.Left;
        if (window.Top < area.Top)
            window.Top = area.Top;
        if (window.Left + window.Width > area.Right)
            window.Left = area.Right - window.Width;
        if (window.Top + window.Height > area.Bottom)
            window.Top = area.Bottom - window.Height;
    }
}
