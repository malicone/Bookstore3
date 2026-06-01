using System.Windows;
using System.Windows.Input;

namespace Bookstore3.WPF;

internal static class ToolbarShortcutHelper
{
    public static string FormatShortcut(Key key, ModifierKeys modifiers = ModifierKeys.Control)
    {
        if (modifiers == ModifierKeys.None)
            return key.ToString();

        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        parts.Add(key.ToString());
        return string.Join('+', parts);
    }

    public static string FormatToolTip(string action, Key key, ModifierKeys modifiers = ModifierKeys.Control) =>
        $"{ToSentenceCase(action)} ({FormatShortcut(key, modifiers)})";

    public static string ToSentenceCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var lower = text.ToLowerInvariant();
        return char.ToUpper(lower[0]) + lower[1..];
    }

    public static string FormatButtonLabel(string label, Key key, ModifierKeys modifiers = ModifierKeys.None) =>
        $"{label} ({FormatShortcut(key, modifiers)})";

    public static void Register(Window window, Key key, RoutedEventHandler handler, ModifierKeys modifiers = ModifierKeys.Control)
    {
        var command = new RoutedUICommand();
        window.CommandBindings.Add(new CommandBinding(command, (_, e) => handler(window, e)));
        window.InputBindings.Add(new KeyBinding(command, key, modifiers));
    }
}
