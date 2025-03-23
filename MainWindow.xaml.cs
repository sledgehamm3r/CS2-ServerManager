using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using System.Windows.Input;

namespace CS2ServerManager
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<ServerInstance> ServerInstances { get; set; }
        private ServerManager serverManager;
        private const string ServerInstancesFile = "server_instances.json";

        private bool _isProgressVisible;
        public bool IsProgressVisible
        {
            get => _isProgressVisible;
            set
            {
                _isProgressVisible = value;
                OnPropertyChanged();
            }
        }

        private readonly string currentVersion = "1.0.0";

        public MainWindow()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string baseFolder = Path.Combine(documentsPath, "CS2ServerManager");
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }
            InitializeComponent();
            DataContext = this;
            serverManager = new ServerManager();
            ServerInstances = new ObservableCollection<ServerInstance>();
            LoadServerInstances();
            ServerDataGrid.ItemsSource = ServerInstances;
            IsProgressVisible = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void LoadServerSettings(ServerInstance instance)
        {
            string serverFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CS2ServerManager", instance.Name);
            string settingsPath = Path.Combine(serverFolder, "settings.json");

            if (File.Exists(settingsPath))
            {
                var settings = JsonSerializer.Deserialize<ServerSettings>(File.ReadAllText(settingsPath));
                if (settings != null)
                {
                    instance.Name = settings.Name;
                    instance.Port = settings.Port;
                    instance.Map = settings.Map;
                    instance.GameMode = settings.GameMode;
                    instance.SteamAccountToken = settings.SteamAccountToken;
                    instance.TickRate = settings.TickRate;
                    instance.MaxPlayers = settings.MaxPlayers;
                    instance.Insecure = settings.Insecure;
                }
            }
            else
            {
                MessageBox.Show($"Settings file not found for server {instance.Name}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await serverManager.InitializeSteamCmdAsync(UpdateProgress);
            await HideProgressBarAfterDelay();
            await CheckForUpdatesAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CS2ServerManager", currentVersion));
                var response = await client.GetStringAsync("https://api.github.com/repos/sledgehamm3r/CS2-ServerManager/releases/latest");
                using JsonDocument json = JsonDocument.Parse(response);
                string latestVersion = json.RootElement.GetProperty("tag_name").GetString() ?? string.Empty;
                if (!string.IsNullOrEmpty(latestVersion))
                {
                    string cleanLatest = latestVersion.StartsWith("v", StringComparison.InvariantCultureIgnoreCase)
                        ? latestVersion.Substring(1)
                        : latestVersion;
                    if (Version.Parse(cleanLatest) > Version.Parse(currentVersion))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            FooterStatusText.Inlines.Clear();
                            Hyperlink updateLink = new Hyperlink(new Run("New Update available!"))
                            {
                                NavigateUri = new Uri("https://github.com/sledgehamm3r/CS2-ServerManager")
                            };
                            updateLink.RequestNavigate += Hyperlink_RequestNavigate;
                            FooterStatusText.Inlines.Add(updateLink);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Update check failed: " + ex.Message);
            }
        }

        private async void UpdateProgress(int progressPercentage, string status)
        {
            Dispatcher.Invoke(() =>
            {
                if (progressPercentage >= 0)
                {
                    FooterProgressBar.Value = progressPercentage;
                    FooterStatusText.Text = status;
                    IsProgressVisible = true;
                }
                else if (progressPercentage == -1)
                {
                    FooterConsoleTextBox.AppendText(status + Environment.NewLine);
                    FooterConsoleTextBox.ScrollToEnd();
                }
            });

            if (progressPercentage == 100)
            {
                await Task.Delay(2000);
                Dispatcher.Invoke(() =>
                {
                    IsProgressVisible = false;
                    FooterStatusText.Text = "";
                    FooterConsoleTextBox.Clear();
                });
            }
        }

        private async Task HideProgressBarAfterDelay()
        {
            await Task.Delay(2000);
            Dispatcher.Invoke(() =>
            {
                IsProgressVisible = false;
                FooterStatusText.Text = "";
                FooterConsoleTextBox.Clear();
            });
        }

        private async void DownloadServerFiles_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedServerInstance(sender) is ServerInstance instance)
            {
                var result = MessageBox.Show("Do you want to install / reinstall the server?", "Confirm Download", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    bool downloadResult = await serverManager.DownloadServerFilesAsync(instance.Name, UpdateProgress);
                    if (downloadResult)
                    {
                        MessageBox.Show("Server files downloaded/updated successfully.", "Success",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                        HideDownloadButton(sender);
                    }
                    else
                    {
                        MessageBox.Show("Failed to download server files: " + serverManager.LastError, "Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a server.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void HideDownloadButton(object sender)
        {
            if (sender is Button button)
            {
                button.Visibility = Visibility.Collapsed;
            }
        }

        private void CreateServer_Click(object sender, RoutedEventArgs e)
        {
            string name = "CS2 Server " + (ServerInstances.Count + 1);
            int port = 27015 + ServerInstances.Count;
            ServerInstance instance = serverManager.CreateServer(name, port);
            ServerInstances.Add(instance);
            MessageBox.Show("Server created: " + name, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void StartServer_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedServerInstance(sender) is ServerInstance instance)
            {
                try
                {
                    IsProgressVisible = true;
                    FooterStatusText.Text = $"Starting server {instance.Name}...";

                    var result = await instance.StartServerWithTimeoutAsync();

                    if (result.Success)
                    {
                        instance.Status = "Running";
                        ServerDataGrid.Items.Refresh();
                        MessageBox.Show($"Server started: {instance.Name}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to start server: {result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error starting server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsProgressVisible = false;
                    FooterStatusText.Text = "";
                }
            }
            else
            {
                MessageBox.Show("Please select a server.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StopServer_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedServerInstance(sender) is ServerInstance instance)
            {
                if (serverManager.StopServer(instance))
                {
                    instance.Status = "Stopped";
                    ServerDataGrid.Items.Refresh();
                    MessageBox.Show("Server stopped: " + instance.Name, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to stop server: " + instance.Name, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a server.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteServer_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedServerInstance(sender) is ServerInstance instance)
            {
                if (instance.Status == "Running")
                {
                    MessageBox.Show("Please stop the running server first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to delete the server '{instance.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    ServerInstances.Remove(instance);
                    MessageBox.Show("Server deleted: " + instance.Name, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Please select a server.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EditServer_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedServerInstance(sender) is ServerInstance instance)
            {
                if (instance.Status == "Running")
                {
                    MessageBox.Show("Please stop the running server first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string oldName = instance.Name;

                EditServerWindow editWindow = new EditServerWindow(instance, serverManager);
                if (editWindow.ShowDialog() == true)
                {
                    serverManager.UpdateServerSettings(instance, oldName);
                    ServerDataGrid.Items.Refresh();
                    SaveServerInstances();
                    LoadServerSettings(instance);
                    MessageBox.Show("Server settings updated.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Please select a server.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var instance in ServerInstances)
            {
                if (instance.ServerProcess != null && instance.ServerProcess.StartInfo != null && instance.ServerProcess.StartInfo.FileName != "" && !instance.ServerProcess.HasExited)
                {
                    try
                    {
                        instance.ServerProcess.Kill();
                    }
                    catch { }
                }
            }
            serverManager.StopAllProcesses();
            SaveServerInstances();
        }


        private void ToggleConsole_Click(object sender, RoutedEventArgs e)
        {
            FooterPanel.Visibility = FooterPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadServerInstances()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CS2ServerManager", ServerInstancesFile);
            if (File.Exists(filePath))
            {
                var instances = JsonSerializer.Deserialize<List<ServerInstance>>(File.ReadAllText(filePath));
                if (instances != null)
                {
                    foreach (var instance in instances)
                    {
                        instance.ServerProcess = null!;
                        ServerInstances.Add(instance);
                    }
                    Console.WriteLine("Loaded server instances from JSON file.");
                }
            }
            else
            {
                Console.WriteLine("No JSON file found. No server instances loaded.");
            }
        }

        private void SaveServerInstances()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CS2ServerManager", ServerInstancesFile);
            var instances = new List<ServerInstance>(ServerInstances);
            File.WriteAllText(filePath, JsonSerializer.Serialize(instances, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Saved server instances to JSON file at {filePath}.");
        }

        private ServerInstance? GetSelectedServerInstance(object sender)
        {
            if (sender is FrameworkElement element && element.DataContext is ServerInstance instance)
            {
                return instance;
            }
            return null;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }

        private void CreditsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CreditsWindow creditsWindow = new CreditsWindow
            {
                Owner = this
            };
            creditsWindow.ShowDialog();
        }

        private void GithubMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/sledgehamm3r/CS2-ServerManager") { UseShellExecute = true });
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
