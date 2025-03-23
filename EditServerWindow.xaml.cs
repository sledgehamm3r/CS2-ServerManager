using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Win32;
using System.ComponentModel;
using System.Xml;
using System.Text.Json;

namespace CS2ServerManager
{
    public partial class EditServerWindow : Window
    {
        private ServerInstance _serverInstance;
        private ServerManager _serverManager;
        private DispatcherTimer _configAutoSaveTimer;
        private bool _isConfigChanged = false;
        private string _originalConfigContent = string.Empty;
        private bool _isServerRunning = false;
        private CancellationTokenSource _pluginOperationCts;

        private TextBox _rconPasswordTextBox;
        private TextBox _serverTagsTextBox;
        private CheckBox _autoRestartCheckBox;
        private TextBlock _autoSaveIndicatorText;
        private TextBox _rconCommandTextBox;
        private TextBox _rconOutputTextBox;
        private Button _sendRconButton;
        private Button _downloadWorkshopMapButton;
        private Button _backupButton;

        public EditServerWindow(ServerInstance serverInstance, ServerManager serverManager)
        {
            try
            {
                InitializeComponent();
                Debug.WriteLine("Initializing EditServerWindow.");
                _serverInstance = serverInstance ?? throw new ArgumentNullException(nameof(serverInstance));
                _serverManager = serverManager ?? throw new ArgumentNullException(nameof(serverManager));
                _pluginOperationCts = new CancellationTokenSource();

                Title = $"Edit CS2 Server: {_serverInstance.Name}";

                InitializeAdditionalUI();
                InitializeUIWithServerData();
                SetupConfigAutoSave();
                LoadAvailableMaps();
                EnsureConfigDirectoryExists();
                LoadConfigContent();
                SetupRconConsole();
                CheckIfServerIsRunning();
                ValidateInputs(showWarningsOnly: true);

                Closing += EditServerWindow_Closing;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing EditServerWindow: {ex.Message}");
                MessageBox.Show($"Error initializing window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditServerWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_isConfigChanged)
            {
                var result = MessageBox.Show(
                    "There are unsaved changes. Do you want to save them before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (!SaveCurrentConfig())
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _pluginOperationCts.Cancel();
        }

        private void InitializeAdditionalUI()
        {
            try
            {
                _rconPasswordTextBox = FindName("RconPasswordTextBox") as TextBox ?? throw new InvalidOperationException("RconPasswordTextBox not found.");
                _serverTagsTextBox = FindName("ServerTagsTextBox") as TextBox ?? throw new InvalidOperationException("ServerTagsTextBox not found.");
                _autoRestartCheckBox = FindName("AutoRestartCheckBox") as CheckBox ?? throw new InvalidOperationException("AutoRestartCheckBox not found.");
                _autoSaveIndicatorText = FindName("AutoSaveIndicatorText") as TextBlock ?? throw new InvalidOperationException("AutoSaveIndicatorText not found.");
                _rconCommandTextBox = FindName("RconCommandTextBox") as TextBox ?? throw new InvalidOperationException("RconCommandTextBox not found.");
                _rconOutputTextBox = FindName("RconOutputTextBox") as TextBox ?? throw new InvalidOperationException("RconOutputTextBox not found.");
                _sendRconButton = FindName("SendRconButton") as Button ?? throw new InvalidOperationException("SendRconButton not found.");
                _downloadWorkshopMapButton = FindName("DownloadWorkshopMapButton") as Button ?? throw new InvalidOperationException("DownloadWorkshopMapButton not found.");
                _backupButton = FindName("BackupButton") as Button ?? throw new InvalidOperationException("BackupButton not found.");

                if (_autoSaveIndicatorText == null && ConfigTextBox != null)
                {
                    Grid parentGrid = ConfigTextBox.Parent as Grid;
                    if (parentGrid != null)
                    {
                        _autoSaveIndicatorText = new TextBlock
                        {
                            Text = "Ready",
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(0, -20, 0, 0),
                            FontSize = 10
                        };

                        Grid.SetRow(_autoSaveIndicatorText, Grid.GetRow(ConfigTextBox));
                        parentGrid.Children.Add(_autoSaveIndicatorText);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing additional UI elements: {ex.Message}");
            }
        }


        private void InitializeUIWithServerData()
        {
            PortTextBox.Text = _serverInstance.Port.ToString();
            GameModeTextBox.Text = _serverInstance.GameMode;
            SteamAccountTokenTextBox.Text = _serverInstance.SteamAccountToken;
            MaxPlayersTextBox.Text = _serverInstance.MaxPlayers.ToString();
            InsecureCheckBox.IsChecked = _serverInstance.Insecure;

            if (_rconPasswordTextBox != null)
                _rconPasswordTextBox.Text = _serverInstance.RconPassword;

            if (_serverTagsTextBox != null)
                _serverTagsTextBox.Text = _serverInstance.ServerTags;

            if (_autoRestartCheckBox != null)
                _autoRestartCheckBox.IsChecked = _serverInstance.AutoRestart;

            PracConfigCheckBox.Checked += ConfigCheckBox_Checked;
            FiveVFiveConfigCheckBox.Checked += ConfigCheckBox_Checked;
            TwoVTwoConfigCheckBox.Checked += ConfigCheckBox_Checked;
            DeathmatchConfigCheckBox.Checked += ConfigCheckBox_Checked;

            InitializeMapSelection();

            if (_serverInstance.CounterStrikeSharpInstalled)
            {
                InstallCounterStrikeSharpButton.Visibility = Visibility.Collapsed;
                PluginManagerPanel.Visibility = Visibility.Visible;
                RefreshPluginList();
            }

            SetSelectedConfig();
        }

        private void InitializeMapSelection()
        {
            try
            {
                var selectedItem = MapComboBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => item.Content.ToString() == _serverInstance.Map);

                if (selectedItem == null)
                {
                    selectedItem = MapComboBox.Items.Cast<ComboBoxItem>()
                        .FirstOrDefault(item => item.Content.ToString() == "custom");

                    if (selectedItem != null)
                    {
                        MapComboBox.SelectedItem = selectedItem;
                        CustomMapTextBox.Text = string.IsNullOrWhiteSpace(_serverInstance.Map)
                            ? "Map ID"
                            : _serverInstance.Map;
                        CustomMapTextBox.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        var customItem = new ComboBoxItem
                        {
                            Content = "custom",
                            Tag = "workshop"
                        };
                        MapComboBox.Items.Add(customItem);
                        MapComboBox.SelectedItem = customItem;
                        CustomMapTextBox.Text = string.IsNullOrWhiteSpace(_serverInstance.Map)
                            ? "Map ID"
                            : _serverInstance.Map;
                        CustomMapTextBox.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    MapComboBox.SelectedItem = selectedItem;
                    CustomMapTextBox.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing map selection: {ex.Message}");
                CustomMapTextBox.Text = _serverInstance.Map;
                CustomMapTextBox.Visibility = Visibility.Visible;
            }
        }

        private void SetSelectedConfig()
        {
            PracConfigCheckBox.IsChecked = false;
            FiveVFiveConfigCheckBox.IsChecked = false;
            TwoVTwoConfigCheckBox.IsChecked = false;
            DeathmatchConfigCheckBox.IsChecked = false;

            if (!string.IsNullOrEmpty(_serverInstance.SelectedConfig))
            {
                switch (_serverInstance.SelectedConfig)
                {
                    case "prac.cfg":
                        PracConfigCheckBox.IsChecked = true;
                        break;
                    case "5v5.cfg":
                        FiveVFiveConfigCheckBox.IsChecked = true;
                        break;
                    case "2v2.cfg":
                        TwoVTwoConfigCheckBox.IsChecked = true;
                        break;
                    case "deathmatch.cfg":
                        DeathmatchConfigCheckBox.IsChecked = true;
                        break;
                    default:
                        break;
                }
            }
        }

        private void SetupConfigAutoSave()
        {
            _configAutoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _configAutoSaveTimer.Tick += ConfigAutoSave_Tick;

            if (ConfigTextBox != null)
            {
                ConfigTextBox.TextChanged += (s, e) =>
                {
                    if (!_isConfigChanged && _originalConfigContent != ConfigTextBox.Text)
                    {
                        _isConfigChanged = true;
                        if (_autoSaveIndicatorText != null)
                        {
                            _autoSaveIndicatorText.Text = "Unsaved changes";
                            _autoSaveIndicatorText.Foreground = Brushes.Orange;
                        }
                    }

                    _configAutoSaveTimer.Stop();
                    _configAutoSaveTimer.Start();
                };
            }
        }

        private void ConfigAutoSave_Tick(object sender, EventArgs e)
        {
            _configAutoSaveTimer.Stop();
            if (_isConfigChanged)
            {
                SaveConfigDraft();
                if (_autoSaveIndicatorText != null)
                {
                    _autoSaveIndicatorText.Text = "Auto-saved";
                    _autoSaveIndicatorText.Foreground = Brushes.Green;
                }
                _isConfigChanged = false;
            }
        }

        private void SaveConfigDraft()
        {
            if (ConfigTextBox == null)
                return;

            try
            {
                string configPath = GetConfigPathByName("server.cfg");
                string directory = Path.GetDirectoryName(configPath);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(configPath + ".draft", ConfigTextBox.Text);
                Debug.WriteLine("Config draft autosaved.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save config draft: {ex.Message}");
            }
        }

        private void LoadAvailableMaps()
        {
            try
            {
                string mapDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "CS2ServerManager",
                    _serverInstance.Name,
                    "CS2ServerFiles",
                    "game",
                    "csgo",
                    "maps");

                if (Directory.Exists(mapDirectory))
                {
                    var existingItems = MapComboBox.Items.Cast<ComboBoxItem>()
                        .Where(item => item.Tag?.ToString() == "standard")
                        .ToList();

                    var customItem = MapComboBox.Items.Cast<ComboBoxItem>()
                        .FirstOrDefault(item => item.Content.ToString() == "custom");

                    MapComboBox.Items.Clear();
                    foreach (var item in existingItems)
                    {
                        MapComboBox.Items.Add(item);
                    }

                    var mapFiles = new List<string>();

                    if (Directory.Exists(mapDirectory))
                    {
                        mapFiles.AddRange(Directory.GetFiles(mapDirectory, "*.bsp"));
                    }

                    string workshopMapDirectory = Path.Combine(mapDirectory, "workshop");
                    if (Directory.Exists(workshopMapDirectory))
                    {
                        foreach (var workshopFolder in Directory.GetDirectories(workshopMapDirectory))
                        {
                            if (Directory.Exists(workshopFolder))
                            {
                                mapFiles.AddRange(Directory.GetFiles(workshopFolder, "*.bsp"));
                            }
                        }
                    }

                    foreach (var mapFile in mapFiles)
                    {
                        string mapName = Path.GetFileNameWithoutExtension(mapFile);
                        if (!existingItems.Any(i => i.Content.ToString() == mapName))
                        {
                            var item = new ComboBoxItem
                            {
                                Content = mapName,
                                Tag = "installed"
                            };
                            MapComboBox.Items.Add(item);
                        }
                    }

                    if (customItem != null)
                    {
                        MapComboBox.Items.Add(customItem);
                    }
                    else
                    {
                        MapComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = "custom",
                            Tag = "workshop"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading map list: {ex.Message}");
            }
        }

        private void EnsureConfigDirectoryExists()
        {
            string cfgFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "CS2ServerManager",
                _serverInstance.Name,
                "CS2ServerFiles",
                "game",
                "csgo",
                "cfg");

            if (!Directory.Exists(cfgFolder))
            {
                try
                {
                    Directory.CreateDirectory(cfgFolder);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating config directory: {ex.Message}");
                    MessageBox.Show($"Error creating config directory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SetupRconConsole()
        {
            if (_rconCommandTextBox != null && _rconOutputTextBox != null && _sendRconButton != null)
            {
                _sendRconButton.Click += SendRconCommand_Click;
                _rconCommandTextBox.KeyDown += (s, e) => {
                    if (e.Key == System.Windows.Input.Key.Enter)
                    {
                        SendRconCommand_Click(s, e);
                        e.Handled = true;
                    }
                };

                _rconOutputTextBox.AppendText("RCON Console initialized. Server must be running to use commands.\n");
                _rconOutputTextBox.AppendText($"Server status: {(_serverInstance.IsRunning ? "Running" : "Stopped")}\n");

                UpdateRconConsoleState();
            }
        }

        private void CheckIfServerIsRunning()
        {
            _isServerRunning = _serverInstance.IsRunning;
            UpdateRconConsoleState();
        }

        private void UpdateRconConsoleState()
        {
            if (_rconCommandTextBox != null && _rconOutputTextBox != null && _sendRconButton != null)
            {
                bool enabled = _isServerRunning && !string.IsNullOrEmpty(_serverInstance.RconPassword);
                _rconCommandTextBox.IsEnabled = enabled;
                _sendRconButton.IsEnabled = enabled;

                if (!_isServerRunning)
                {
                    _rconOutputTextBox.AppendText("Server is not running. Start the server to use RCON commands.\n");
                }
                else if (string.IsNullOrEmpty(_serverInstance.RconPassword))
                {
                    _rconOutputTextBox.AppendText("No RCON password set. Please configure an RCON password.\n");
                }
            }
        }

        private async void SendRconCommand_Click(object sender, RoutedEventArgs e)
        {
            if (_rconCommandTextBox == null || _rconOutputTextBox == null)
                return;

            if (string.IsNullOrWhiteSpace(_rconCommandTextBox.Text))
                return;

            if (string.IsNullOrEmpty(_serverInstance.RconPassword))
            {
                MessageBox.Show("RCON password is not configured.",
                    "RCON Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_serverInstance.IsRunning)
            {
                MessageBox.Show("Server is not running.",
                    "RCON Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string command = _rconCommandTextBox.Text;
                _rconCommandTextBox.Clear();
                _rconOutputTextBox.AppendText($"> {command}\n");

                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

                string response = await _serverManager.SendRconCommandAsync(
                    _serverInstance, command);

                _rconOutputTextBox.AppendText($"{response}\n");
                _rconOutputTextBox.ScrollToEnd();
            }
            catch (Exception ex)
            {
                _rconOutputTextBox.AppendText($"Error: {ex.Message}\n");
                _rconOutputTextBox.ScrollToEnd();
            }
        }

        private bool ValidateInputs(bool showWarningsOnly = false)
        {
            bool isValid = true;
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            if (!int.TryParse(PortTextBox.Text, out int port) || port < 1024 || port > 65535)
            {
                errors.Add("Port must be a number between 1024 and 65535.");
                isValid = false;
            }

            if (!int.TryParse(MaxPlayersTextBox.Text, out int players) || players < 1 || players > 64)
            {
                errors.Add("Player count must be between 1 and 64.");
                isValid = false;
            }

            var selectedItem = MapComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
            {
                errors.Add("Please select a map.");
                isValid = false;
            }
            else if (selectedItem.Content.ToString() == "custom")
            {
                if (string.IsNullOrWhiteSpace(CustomMapTextBox.Text) || CustomMapTextBox.Text == "Map ID")
                {
                    errors.Add("You must specify a Map ID when selecting a custom map.");
                    isValid = false;
                }
                else if (!long.TryParse(CustomMapTextBox.Text, out _))
                {
                    errors.Add("Workshop Map ID must be a valid number.");
                    isValid = false;
                }
            }

            if (string.IsNullOrWhiteSpace(SteamAccountTokenTextBox.Text) ||
                SteamAccountTokenTextBox.Text == "YOURLOGINTOKEN")
            {
                warnings.Add("A Steam account token is required for public servers.");
            }

            if (_rconPasswordTextBox != null && string.IsNullOrWhiteSpace(_rconPasswordTextBox.Text))
            {
                warnings.Add("Without an RCON password, remote server management is not possible.");
            }

            if (errors.Count > 0 && !showWarningsOnly)
            {
                MessageBox.Show(
                    string.Join("\n", errors),
                    "Invalid Inputs",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            if (warnings.Count > 0)
            {
                MessageBox.Show(
                    string.Join("\n", warnings),
                    "Warnings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            return isValid;
        }

        private void ConfigCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox changed)
            {
                if (_isConfigChanged)
                {
                    var result = MessageBox.Show(
                        "There are unsaved changes in the current configuration. Do you want to save them before loading a different configuration?",
                        "Unsaved Changes",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        SaveCurrentConfig();
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        changed.IsChecked = false;
                        return;
                    }
                }

                UnselectOtherConfigCheckboxes(changed);
                LoadSelectedConfig();
            }
        }
        private void UnselectOtherConfigCheckboxes(CheckBox selectedCheckbox)
        {
            if (selectedCheckbox == PracConfigCheckBox)
            {
                FiveVFiveConfigCheckBox.IsChecked = false;
                TwoVTwoConfigCheckBox.IsChecked = false;
                DeathmatchConfigCheckBox.IsChecked = false;
            }
            else if (selectedCheckbox == FiveVFiveConfigCheckBox)
            {
                PracConfigCheckBox.IsChecked = false;
                TwoVTwoConfigCheckBox.IsChecked = false;
                DeathmatchConfigCheckBox.IsChecked = false;
            }
            else if (selectedCheckbox == TwoVTwoConfigCheckBox)
            {
                PracConfigCheckBox.IsChecked = false;
                FiveVFiveConfigCheckBox.IsChecked = false;
                DeathmatchConfigCheckBox.IsChecked = false;
            }
            else if (selectedCheckbox == DeathmatchConfigCheckBox)
            {
                PracConfigCheckBox.IsChecked = false;
                FiveVFiveConfigCheckBox.IsChecked = false;
                TwoVTwoConfigCheckBox.IsChecked = false;
            }
        }

        private void LoadSelectedConfig()
        {
            string configPath = GetSelectedConfigPath();
            if (File.Exists(configPath))
            {
                try
                {
                    ConfigTextBox.Text = File.ReadAllText(configPath);
                    _originalConfigContent = ConfigTextBox.Text;
                    _isConfigChanged = false;
                    if (_autoSaveIndicatorText != null)
                    {
                        _autoSaveIndicatorText.Text = "Configuration loaded";
                        _autoSaveIndicatorText.Foreground = Brushes.Green;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading config: {ex.Message}");
                    MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                CreateConfigFromTemplate(configPath);
            }
        }

        private string GetSelectedConfigPath()
        {
            string configName = "server.cfg";

            if (PracConfigCheckBox.IsChecked == true)
                configName = "prac.cfg";
            else if (FiveVFiveConfigCheckBox.IsChecked == true)
                configName = "5v5.cfg";
            else if (TwoVTwoConfigCheckBox.IsChecked == true)
                configName = "2v2.cfg";
            else if (DeathmatchConfigCheckBox.IsChecked == true)
                configName = "deathmatch.cfg";

            return GetConfigPathByName(configName);
        }

        private void CreateConfigFromTemplate(string configPath)
        {
            try
            {
                string templateName = Path.GetFileName(configPath);
                string sourceConfigsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Configs");
                string sourceTemplate = Path.Combine(sourceConfigsDir, templateName);

                if (File.Exists(sourceTemplate))
                {
                    string directory = Path.GetDirectoryName(configPath);
                    if (directory != null && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.Copy(sourceTemplate, configPath);
                    ConfigTextBox.Text = File.ReadAllText(configPath);
                    _originalConfigContent = ConfigTextBox.Text;
                    _isConfigChanged = false;
                    Debug.WriteLine($"Created config {templateName} from template.");
                }
                else
                {
                    Debug.WriteLine($"Template {templateName} not found in {sourceConfigsDir}");

                    string defaultConfig = GetDefaultConfigContent(templateName);

                    string directory = Path.GetDirectoryName(configPath);
                    if (directory != null && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(configPath, defaultConfig);
                    ConfigTextBox.Text = defaultConfig;
                    _originalConfigContent = defaultConfig;
                    _isConfigChanged = false;
                    Debug.WriteLine($"Created new default configuration for {Path.GetFileName(configPath)}");
                }

                if (_autoSaveIndicatorText != null)
                {
                    _autoSaveIndicatorText.Text = "New configuration created";
                    _autoSaveIndicatorText.Foreground = Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating config from template: {ex.Message}");
                MessageBox.Show($"Error creating configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private string GetDefaultConfigContent(string configName)
        {
            switch (configName)
            {
                case "prac.cfg":
                    return "// Practice Mode Configuration\r\n" +
                           "hostname \"CS2 Practice Server\"\r\n" +
                           "sv_cheats 1\r\n" +
                           "mp_limitteams 0\r\n" +
                           "mp_autoteambalance 0\r\n" +
                           "mp_maxmoney 65000\r\n" +
                           "mp_startmoney 65000\r\n" +
                           "mp_freezetime 0\r\n" +
                           "mp_round_restart_delay 0\r\n" +
                           "mp_buytime 9999\r\n" +
                           "sv_infinite_ammo 2\r\n" +
                           "sv_showimpacts 1\r\n" +
                           "sv_grenade_trajectory 1\r\n";

                case "5v5.cfg":
                    return "// 5v5 Competitive Configuration\r\n" +
                           "hostname \"CS2 5v5 Server\"\r\n" +
                           "mp_freezetime 15\r\n" +
                           "mp_maxrounds 30\r\n" +
                           "mp_halftime 1\r\n" +
                           "mp_match_can_clinch 1\r\n" +
                           "mp_overtime_enable 1\r\n" +
                           "mp_overtime_maxrounds 6\r\n" +
                           "mp_autoteambalance 0\r\n" +
                           "mp_limitteams 0\r\n" +
                           "mp_warmuptime 60\r\n" +
                           "mp_match_end_restart 1\r\n";

                case "2v2.cfg":
                    return "// 2v2 Wingman Configuration\r\n" +
                           "hostname \"CS2 2v2 Server\"\r\n" +
                           "mp_freezetime 10\r\n" +
                           "mp_maxrounds 16\r\n" +
                           "mp_halftime 0\r\n" +
                           "mp_autoteambalance 0\r\n" +
                           "mp_limitteams 0\r\n" +
                           "mp_warmuptime 30\r\n" +
                           "mp_match_end_restart 1\r\n";

                case "deathmatch.cfg":
                    return "// Deathmatch Configuration\r\n" +
                           "hostname \"CS2 Deathmatch Server\"\r\n" +
                           "mp_freezetime 0\r\n" +
                           "mp_roundtime 10\r\n" +
                           "mp_timelimit 20\r\n" +
                           "mp_autoteambalance 1\r\n" +
                           "mp_buytime 9999\r\n" +
                           "mp_respawn_on_death_t 1\r\n" +
                           "mp_respawn_on_death_ct 1\r\n" +
                           "mp_respawn_immunitytime 0\r\n";

                default:
                    return "// Default CS2 server configuration\r\n" +
                           "hostname \"CS2 Dedicated Server\"\r\n" +
                           "sv_cheats 0\r\n" +
                           "sv_lan 0\r\n" +
                           "sv_allow_votes 1\r\n" +
                           "sv_voiceenable 1\r\n" +
                           "mp_friendlyfire 0\r\n" +
                           "mp_autoteambalance 1\r\n" +
                           "mp_autokick 0\r\n" +
                           "mp_freeze_period 5\r\n" +
                           "sv_downloadurl \"\"\r\n" +
                           "sv_steamauth_enforce 1\r\n" +
                           "sv_password \"\"\r\n" +
                           "sv_region 3\r\n" +
                           "mp_match_end_restart 1\r\n";
            }
        }

        public void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MapComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = MapComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Content.ToString() == "custom")
            {
                if (string.IsNullOrWhiteSpace(CustomMapTextBox.Text) || CustomMapTextBox.Text == "Map ID")
                {
                    CustomMapTextBox.Text = "Map ID";
                }
                CustomMapTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                CustomMapTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConfigChanged)
            {
                var result = MessageBox.Show(
                    "There are unsaved changes. Do you want to exit anyway?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            this.DialogResult = false;
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
            {
                return;
            }

            try
            {
                if (!int.TryParse(PortTextBox.Text, out int port))
                {
                    MessageBox.Show("Invalid port number", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _serverInstance.Port = port;

                _serverInstance.Map = ((ComboBoxItem)MapComboBox.SelectedItem)?.Content.ToString() == "custom"
                    ? CustomMapTextBox.Text
                    : ((ComboBoxItem)MapComboBox.SelectedItem)?.Content.ToString() ?? string.Empty;

                _serverInstance.GameMode = GameModeTextBox.Text;
                _serverInstance.SteamAccountToken = SteamAccountTokenTextBox.Text;

                if (!int.TryParse(MaxPlayersTextBox.Text, out int maxPlayers))
                {
                    MessageBox.Show("Invalid player count", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _serverInstance.MaxPlayers = maxPlayers;

                _serverInstance.Insecure = InsecureCheckBox.IsChecked ?? false;

                if (_rconPasswordTextBox != null)
                    _serverInstance.RconPassword = _rconPasswordTextBox.Text;

                if (_serverTagsTextBox != null)
                    _serverInstance.ServerTags = _serverTagsTextBox.Text;

                if (_autoRestartCheckBox != null)
                    _serverInstance.AutoRestart = _autoRestartCheckBox.IsChecked ?? false;

                DetermineSelectedConfig();

                if (!SaveCurrentConfig())
                {
                    return;
                }

                _serverInstance.LastModifiedDate = DateTime.Now;
                _serverManager.UpdateServerSettings(_serverInstance);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DetermineSelectedConfig()
        {
            if (PracConfigCheckBox.IsChecked == true)
            {
                _serverInstance.SelectedConfig = "prac.cfg";
            }
            else if (FiveVFiveConfigCheckBox.IsChecked == true)
            {
                _serverInstance.SelectedConfig = "5v5.cfg";
            }
            else if (TwoVTwoConfigCheckBox.IsChecked == true)
            {
                _serverInstance.SelectedConfig = "2v2.cfg";
            }
            else if (DeathmatchConfigCheckBox.IsChecked == true)
            {
                _serverInstance.SelectedConfig = "deathmatch.cfg";
            }
            else
            {
                _serverInstance.SelectedConfig = "server.cfg";
            }
        }

        private bool SaveCurrentConfig()
        {
            if (ConfigTextBox == null)
            {
                MessageBox.Show("Error: Config text field not found.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            try
            {
                string configPath = GetSelectedConfigPath();

                string cfgFolder = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(cfgFolder))
                {
                    Directory.CreateDirectory(cfgFolder);
                }

                File.WriteAllText(configPath, ConfigTextBox.Text);
                _originalConfigContent = ConfigTextBox.Text;
                _isConfigChanged = false;

                if (_autoSaveIndicatorText != null)
                {
                    _autoSaveIndicatorText.Text = "Configuration saved";
                    _autoSaveIndicatorText.Foreground = Brushes.Green;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving config file: " + ex.Message);
                MessageBox.Show("Error saving configuration: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private string GetConfigPathByName(string configName)
        {
            string cfgFolder = Path.Combine(
                ServerInstance.ServerPaths.GetServerFolder(_serverInstance.Name),
                "CS2ServerFiles", "game", "csgo", "cfg");

            return Path.Combine(cfgFolder, configName);
        }

        private async void InstallCounterStrikeSharp_Click(object sender, RoutedEventArgs e)
        {
            var userResult = MessageBox.Show(
                "Do you want to install CounterStrikeSharp? This enables the use of plugins for your server.",
                "Install CounterStrikeSharp",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (userResult != MessageBoxResult.Yes)
                return;

            if (_serverInstance.IsRunning)
            {
                MessageBox.Show(
                    "The server must be stopped before installing CounterStrikeSharp.",
                    "Server Running",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            InstallCounterStrikeSharpButton.IsEnabled = false;

            try
            {
                ProgressDialog progressDialog = new ProgressDialog
                {
                    Owner = this,
                    Title = "Installing CounterStrikeSharp"
                };
                progressDialog.Show();

                bool success = await _serverManager.InstallCounterStrikeSharpAsync(_serverInstance, (progress, status) =>
                {
                    progressDialog.UpdateProgress(progress, status);
                });

                progressDialog.Close();

                if (success)
                {
                    MessageBox.Show(
                        "CounterStrikeSharp was successfully installed.",
                        "Installation Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    _serverInstance.CounterStrikeSharpInstalled = true;
                    ShowPluginManager();
                    ValidateCounterStrikeSharpInstallation();
                }
                else
                {
                    MessageBox.Show(
                        "CounterStrikeSharp installation failed: " + _serverManager.LastError,
                        "Installation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during CounterStrikeSharp installation: {ex.Message}",
                    "Installation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                InstallCounterStrikeSharpButton.IsEnabled = true;
            }
        }

        private string GetPluginDirectory()
        {
            return Path.Combine(
                ServerInstance.ServerPaths.GetServerFolder(_serverInstance.Name),
                "CS2ServerFiles", "game", "csgo", "addons");
        }

        public class PluginInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string Version { get; set; } = "Unknown";
            public string Author { get; set; } = "Unknown";
            public bool IsEnabled { get; set; } = true;

            public override string ToString()
            {
                return Name;
            }
        }

        private async void RefreshPluginList()
        {
            try
            {
                PluginListView.IsEnabled = false;

                var plugins = await Task.Run(() => LoadPluginInfos());

                if (_pluginOperationCts.IsCancellationRequested)
                    return;

                PluginListView.ItemsSource = plugins;

                PluginListView.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing plugin list: {ex.Message}");
                MessageBox.Show(
                    $"Error refreshing plugin list: {ex.Message}",
                    "Plugin Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private List<PluginInfo> LoadPluginInfos()
        {
            List<PluginInfo> plugins = new List<PluginInfo>();
            string pluginsRootPath = GetPluginDirectory();
            string cssPluginsPath = Path.Combine(pluginsRootPath, "counterstrikesharp", "plugins");

            try
            {
                if (!Directory.Exists(cssPluginsPath))
                {
                    Debug.WriteLine("CounterStrikeSharp plugins directory not found: " + cssPluginsPath);

                    if (Directory.Exists(pluginsRootPath))
                    {
                        foreach (var dir in Directory.GetDirectories(pluginsRootPath))
                        {
                            plugins.Add(new PluginInfo
                            {
                                Name = new DirectoryInfo(dir).Name,
                                Type = "Addon Directory",
                                Path = dir
                            });
                        }
                    }
                }
                else
                {
                    foreach (var dir in Directory.GetDirectories(cssPluginsPath))
                    {
                        string dirName = new DirectoryInfo(dir).Name;
                        var pluginFiles = Directory.GetFiles(dir, "*.dll").Concat(Directory.GetFiles(dir, "*.cs"));

                        foreach (var file in pluginFiles)
                        {
                            var pluginInfo = ExtractPluginMetadata(file, dir);
                            plugins.Add(pluginInfo);
                        }

                        if (!pluginFiles.Any())
                        {
                            plugins.Add(new PluginInfo
                            {
                                Name = dirName,
                                Type = "Unknown",
                                Path = dir
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading plugins: {ex.Message}");
            }

            if (plugins.Count == 0)
            {
                plugins.Add(new PluginInfo
                {
                    Name = "No plugins installed",
                    Type = "Info",
                    Path = cssPluginsPath
                });
            }

            return plugins;
        }

        private PluginInfo ExtractPluginMetadata(string filePath, string pluginDir)
        {
            var plugin = new PluginInfo
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                Type = Path.GetExtension(filePath).ToLowerInvariant() == ".dll" ? "Compiled (.dll)" : "Script (.cs)",
                Path = pluginDir,
                FileName = Path.GetFileName(filePath)
            };

            try
            {
                string jsonPath = Path.Combine(pluginDir, "plugin.json");
                if (File.Exists(jsonPath))
                {
                    var jsonContent = File.ReadAllText(jsonPath);
                    var pluginData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                    if (pluginData != null)
                    {
                        if (pluginData.TryGetValue("name", out object? name) && name != null)
                            plugin.Name = name.ToString() ?? string.Empty;

                        if (pluginData.TryGetValue("version", out object? version) && version != null)
                            plugin.Version = version.ToString() ?? "Unknown";

                        if (pluginData.TryGetValue("author", out object? author) && author != null)
                            plugin.Author = author.ToString() ?? "Unknown";
                    }
                }
                else
                {
                    string xmlPath = Path.Combine(pluginDir, $"{Path.GetFileNameWithoutExtension(filePath)}.xml");
                    if (File.Exists(xmlPath))
                    {
                        var doc = new XmlDocument();
                        doc.Load(xmlPath);
                        var versionNode = doc.SelectSingleNode("//version");
                        var authorNode = doc.SelectSingleNode("//author");

                        if (versionNode != null)
                            plugin.Version = versionNode.InnerText;

                        if (authorNode != null)
                            plugin.Author = authorNode.InnerText;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading plugin metadata for {filePath}: {ex.Message}");
            }

            return plugin;
        }

        private bool ValidateCounterStrikeSharpInstallation()
        {
            try
            {
                string rootPath = GetPluginDirectory();
                string cssPath = Path.Combine(rootPath, "counterstrikesharp");
                string metamodPath = Path.Combine(rootPath, "metamod");
                string gameInfoPath = Path.Combine(
                    ServerInstance.ServerPaths.GetGameInfoPath(_serverInstance.Name));

                List<string> issues = new List<string>();

                if (!Directory.Exists(rootPath))
                    issues.Add("Addons directory missing");

                if (!Directory.Exists(cssPath))
                    issues.Add("CounterStrikeSharp directory missing");

                if (!Directory.Exists(metamodPath))
                    issues.Add("Metamod directory missing");

                if (!Directory.Exists(Path.Combine(cssPath, "plugins")))
                    issues.Add("CounterStrikeSharp plugins directory missing");

                if (File.Exists(gameInfoPath))
                {
                    string content = File.ReadAllText(gameInfoPath);
                    if (!content.Contains("Game csgo/addons/metamod") && !content.Contains("Game\tcsgo/addons/metamod"))
                        issues.Add("gameinfo.gi does not contain metamod entry");
                }
                else
                {
                    issues.Add("gameinfo.gi not found");
                }

                if (issues.Count > 0)
                {
                    MessageBox.Show(
                        "CounterStrikeSharp installation may be incomplete:\n\n" +
                        string.Join("\n", issues) +
                        "\n\nTry reinstalling CounterStrikeSharp.",
                        "Installation Check",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error validating CounterStrikeSharp installation: {ex.Message}");
                return false;
            }
        }

        private void PluginOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string pluginPath)
                {
                    if (Directory.Exists(pluginPath))
                    {
                        Process.Start(new ProcessStartInfo(pluginPath) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("Plugin directory not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening plugin directory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PluginDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string pluginPath)
                {
                    if (Directory.Exists(pluginPath))
                    {
                        string pluginName = new DirectoryInfo(pluginPath).Name;

                        if (_serverInstance.IsRunning)
                        {
                            MessageBox.Show(
                                "Cannot delete plugins while the server is running. Please stop the server first.",
                                "Server Running",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        var result = MessageBox.Show($"Are you sure you want to delete the plugin '{pluginName}'?",
                            "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.Yes)
                        {
                            try
                            {
                                Directory.Delete(pluginPath, true);
                                RefreshPluginList();
                                MessageBox.Show("Plugin deleted.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error deleting plugin: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Plugin directory not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting plugin: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UploadPluginButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serverInstance.IsRunning)
            {
                MessageBox.Show(
                    "Cannot install plugins while the server is running. Please stop the server first.",
                    "Server Running",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Plugin Files (*.zip, *.dll, *.cs)|*.zip;*.dll;*.cs",
                Title = "Select Plugin File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    string extension = Path.GetExtension(filePath).ToLower();
                    string pluginName = Path.GetFileNameWithoutExtension(filePath);
                    string cssPluginsPath = Path.Combine(GetPluginDirectory(), "counterstrikesharp", "plugins");
                    string destinationPath;

                    if (Directory.Exists(cssPluginsPath))
                    {
                        destinationPath = Path.Combine(cssPluginsPath, pluginName);
                    }
                    else
                    {
                        Directory.CreateDirectory(cssPluginsPath);
                        destinationPath = Path.Combine(cssPluginsPath, pluginName);
                    }

                    if (Directory.Exists(destinationPath))
                    {
                        var overwrite = MessageBox.Show("A plugin with this name already exists. Do you want to overwrite it?",
                            "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (overwrite != MessageBoxResult.Yes)
                        {
                            return;
                        }
                        Directory.Delete(destinationPath, true);
                    }

                    Directory.CreateDirectory(destinationPath);

                    if (extension == ".zip")
                    {
                        ZipFile.ExtractToDirectory(filePath, destinationPath);
                    }
                    else if (extension == ".dll" || extension == ".cs")
                    {
                        File.Copy(filePath, Path.Combine(destinationPath, Path.GetFileName(filePath)), true);
                    }

                    RefreshPluginList();
                    MessageBox.Show("Plugin successfully installed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error installing plugin: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowPluginManager()
        {
            InstallCounterStrikeSharpButton.Visibility = Visibility.Collapsed;
            PluginManagerPanel.Visibility = Visibility.Visible;
            RefreshPluginList();
        }

        private async void DownloadWorkshopMap_Click(object sender, RoutedEventArgs e)
        {
            if (CustomMapTextBox.Text == "Map ID" || string.IsNullOrWhiteSpace(CustomMapTextBox.Text))
            {
                MessageBox.Show("Please enter a valid workshop map ID.",
                    "Invalid Map ID", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!long.TryParse(CustomMapTextBox.Text, out long _))
            {
                MessageBox.Show("The workshop map ID must be a number.",
                    "Invalid Map ID", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_serverInstance.IsRunning)
            {
                MessageBox.Show(
                    "It's recommended to stop the server before downloading workshop maps. Continue anyway?",
                    "Server Running",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
            }

            if (_downloadWorkshopMapButton != null)
                _downloadWorkshopMapButton.IsEnabled = false;

            try
            {
                ProgressDialog progressDialog = new ProgressDialog
                {
                    Owner = this,
                    Title = "Downloading Workshop Map"
                };
                progressDialog.Show();

                bool success = await _serverManager.DownloadWorkshopItemAsync(_serverInstance, CustomMapTextBox.Text, (progress, status) =>
                {
                    progressDialog.UpdateProgress(progress, status);
                });

                progressDialog.Close();

                if (success)
                {
                    MessageBox.Show(
                        "Workshop map successfully downloaded.",
                        "Download Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    LoadAvailableMaps();
                }
                else
                {
                    MessageBox.Show(
                        "Error downloading workshop map: " + _serverManager.LastError,
                        "Download Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error downloading workshop map: {ex.Message}",
                    "Download Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (_downloadWorkshopMapButton != null)
                    _downloadWorkshopMapButton.IsEnabled = true;
            }
        }

        private void LoadConfigContent()
        {
            if (ConfigTextBox == null)
                return;

            string configPath = GetConfigPathByName("server.cfg");

            string cfgFolder = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(cfgFolder))
            {
                try
                {
                    Directory.CreateDirectory(cfgFolder);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error creating config directory: " + ex.Message);
                    MessageBox.Show("Error creating config directory: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                if (File.Exists(configPath))
                {
                    ConfigTextBox.Text = File.ReadAllText(configPath);
                    _originalConfigContent = ConfigTextBox.Text;
                    _isConfigChanged = false;
                }
                else
                {
                    string defaultConfig = GetDefaultConfigContent("server.cfg");

                    File.WriteAllText(configPath, defaultConfig);
                    ConfigTextBox.Text = defaultConfig;
                    _originalConfigContent = defaultConfig;
                    _isConfigChanged = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error handling config: " + ex.Message);
                ConfigTextBox.Text = string.Empty;
                _originalConfigContent = string.Empty;
                _isConfigChanged = false;
                MessageBox.Show("Error handling configuration: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditPracConfig_Click(object sender, RoutedEventArgs e)
        {
            EditConfigFile(GetConfigPathByName("prac.cfg"));
        }

        private void Edit5v5Config_Click(object sender, RoutedEventArgs e)
        {
            EditConfigFile(GetConfigPathByName("5v5.cfg"));
        }

        private void Edit2v2Config_Click(object sender, RoutedEventArgs e)
        {
            EditConfigFile(GetConfigPathByName("2v2.cfg"));
        }

        private void EditDeathmatchConfig_Click(object sender, RoutedEventArgs e)
        {
            EditConfigFile(GetConfigPathByName("deathmatch.cfg"));
        }

        private void EditConfigFile(string configPath)
        {
            string directory = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error creating config directory: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (!File.Exists(configPath))
            {
                try
                {
                    string defaultConfig = GetDefaultConfigContent(Path.GetFileName(configPath));
                    File.WriteAllText(configPath, defaultConfig);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error creating config file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            EditConfigDialog dialog = new EditConfigDialog(configPath);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void LoadServerCfg_Click(object sender, RoutedEventArgs e)
        {
            LoadConfigContent();
            MessageBox.Show("Configuration loaded.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveServerCfg_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigTextBox == null)
                return;

            string configPath = GetConfigPathByName("server.cfg");

            string cfgFolder = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(cfgFolder))
            {
                try
                {
                    Directory.CreateDirectory(cfgFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error creating configuration directory: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                File.WriteAllText(configPath, ConfigTextBox.Text);
                MessageBox.Show("Configuration saved.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                _originalConfigContent = ConfigTextBox.Text;
                _isConfigChanged = false;
                if (_autoSaveIndicatorText != null)
                {
                    _autoSaveIndicatorText.Text = "Configuration saved";
                    _autoSaveIndicatorText.Foreground = Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving configuration: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigTab_GotFocus(object sender, RoutedEventArgs e)
        {
            string configPath = GetConfigPathByName("server.cfg");

            string cfgFolder = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(cfgFolder))
            {
                try
                {
                    Directory.CreateDirectory(cfgFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error creating config directory: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (File.Exists(configPath))
            {
                LoadConfigContent();
            }
            else
            {
                try
                {
                    string defaultConfig = GetDefaultConfigContent("server.cfg");
                    File.WriteAllText(configPath, defaultConfig);
                    ConfigTextBox.Text = defaultConfig;
                    _originalConfigContent = defaultConfig;
                    _isConfigChanged = false;
                    MessageBox.Show("Default server configuration created.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error creating default configuration: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BackupServerConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_backupButton != null)
                {
                    _backupButton.IsEnabled = false;
                    _backupButton.Content = "Creating backup...";
                }

                string backupPath = await _serverManager.BackupServerConfigsAsync(_serverInstance);

                if (!string.IsNullOrEmpty(backupPath))
                {
                    MessageBox.Show(
                        $"Configuration successfully backed up: {Path.GetFileName(backupPath)}",
                        "Backup Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Backup could not be created: " + _serverManager.LastError,
                        "Backup Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (_backupButton != null)
                {
                    _backupButton.IsEnabled = true;
                    _backupButton.Content = "Backup Config";
                }
            }
        }

        private void ExportServerSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Export Server Configuration",
                FileName = $"{_serverInstance.Name}_config.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(_serverInstance, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });
                    File.WriteAllText(saveFileDialog.FileName, json);
                    MessageBox.Show("Server configuration exported successfully.", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting server configuration: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshMapList_Click(object sender, RoutedEventArgs e)
        {
            LoadAvailableMaps();
            MessageBox.Show("Map list refreshed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshPlugins_Click(object sender, RoutedEventArgs e)
        {
            RefreshPluginList();
            ValidateCounterStrikeSharpInstallation();
            MessageBox.Show("Plugin list refreshed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class EditConfigDialog : Window
    {
        protected string _configPath;
        protected TextBox _textBox;

        public EditConfigDialog(string configPath)
        {
            _configPath = configPath;
            Title = $"Edit Configuration: {Path.GetFileName(configPath)}";
            Width = 650;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid();
            this.Content = grid;

            _textBox = new TextBox
            {
                Name = "ConfigEditorTextBox",
                Text = File.Exists(_configPath) ? File.ReadAllText(_configPath) : string.Empty,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                FontSize = 12
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(_textBox, 0);
            grid.Children.Add(_textBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var saveButton = new Button { Content = "Save", Width = 90, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Cancel", Width = 90, Margin = new Thickness(5) };

            saveButton.Click += SaveButton_Click;
            cancelButton.Click += CancelButton_Click;

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);
        }

        protected virtual void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string directory = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_configPath, _textBox.Text);
                MessageBox.Show("Configuration saved.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving configuration: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_configPath) && _textBox.Text != File.ReadAllText(_configPath))
            {
                var result = MessageBox.Show("You have unsaved changes. Are you sure you want to discard them?",
                    "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            this.DialogResult = false;
            this.Close();
        }
    }
}
