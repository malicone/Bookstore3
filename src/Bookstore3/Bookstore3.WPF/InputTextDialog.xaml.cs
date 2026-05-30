using System.Windows;

namespace Bookstore3.WPF;

/// <summary>
/// Interaction logic for InputTextDialog.xaml
/// </summary>
public partial class InputTextDialog : Window
{    
    public InputTextDialog(string prompt, string defaultAnswer = "")
    {
        InitializeComponent();
        lblPrompt.Text = prompt;
        txtInput.Text = defaultAnswer;
    }

    public string Answer { get; private set; } = string.Empty;

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        var input = txtInput.Text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            AppUtils.ShowInfoMessage("Please enter a value.");
            txtInput.Focus();
            txtInput.SelectAll();
            return;
        }

        Answer = input;
        DialogResult = true;
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        // Automatically focus the input field and select existing text when shown
        txtInput.Focus();
        txtInput.SelectAll();
    }
}
