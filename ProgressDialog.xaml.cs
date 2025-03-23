using System.Windows;

namespace CS2ServerManager
{
    public partial class ProgressDialog : Window
    {
        public ProgressDialog()
        {
            InitializeComponent();
        }

        public void UpdateProgress(int progress, string status)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = progress;
                StatusTextBlock.Text = status;
            });
        }
    }
}
