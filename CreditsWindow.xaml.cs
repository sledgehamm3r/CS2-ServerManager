using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace CS2ServerManager
{
    public partial class CreditsWindow : Window
    {
        public CreditsWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Hyperlink_RequestNavigate(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is string url)
            {
                OpenUrl(url);
            }
            else if (sender is Hyperlink hyperlink)
            {
                OpenUrl(hyperlink.NavigateUri.ToString());
            }
        }

        private void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url) ||
                !(url.StartsWith("http://") || url.StartsWith("https://")))
            {
                MessageBox.Show("Ungültige URL", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der URL: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
