using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Bookstore3.WPF;

internal static class NativeWindowStyles
{
    private const int GwlStyle = -16;
    private const int WsMinimizeBox = 0x00020000;
    private const int WsMaximizeBox = 0x00010000;

    public static void DisableMinimizeAndMaximizeButtons(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var style = GetWindowLong(hwnd, GwlStyle);
        SetWindowLong(hwnd, GwlStyle, style & ~WsMinimizeBox & ~WsMaximizeBox);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static int GetWindowLong(IntPtr hWnd, int nIndex) =>
        IntPtr.Size == 8
            ? (int)GetWindowLongPtr64(hWnd, nIndex)
            : GetWindowLong32(hWnd, nIndex);

    private static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong) =>
        IntPtr.Size == 8
            ? (int)SetWindowLongPtr64(hWnd, nIndex, new IntPtr(dwNewLong))
            : SetWindowLong32(hWnd, nIndex, dwNewLong);
}
