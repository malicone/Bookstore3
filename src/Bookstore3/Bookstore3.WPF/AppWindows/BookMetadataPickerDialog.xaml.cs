using Bookstore3.WPF.AITools;
using Bookstore3.WPF.Utils;
using System.Windows;
using System.Windows.Input;

namespace Bookstore3.WPF.AppWindows;

public partial class BookMetadataPickerDialog : Window
{
    public BookMetadataPickerDialog(IReadOnlyList<BookMetadataResult> results)
    {
        InitializeComponent();
        ResultsListView.ItemsSource = results;
    }

    public BookMetadataResult? SelectedMetadata { get; private set; }

    private void Window_LoadedHandler(object sender, RoutedEventArgs e)
    {
        if (ResultsListView.Items.Count > 0)
            ResultsListView.SelectedIndex = 0;

        ResultsListView.Focus();
    }

    private void OkButton_ClickHandler(object sender, RoutedEventArgs e) => ConfirmSelection();

    private void ResultsListView_MouseDoubleClickHandler(object sender, MouseButtonEventArgs e) =>
        ConfirmSelection();

    private void ConfirmSelection()
    {
        if (ResultsListView.SelectedItem is not BookMetadataResult selected)
        {
            AppUtils.ShowInfoMessage("Please select a book.");
            return;
        }

        SelectedMetadata = selected;
        DialogResult = true;
    }
}
