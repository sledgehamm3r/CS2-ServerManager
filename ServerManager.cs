using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.IO.Compression;
using NLog;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Text.RegularExpressions;

namespace CS2ServerManager
{
    public enum ServerManagerErrorType
    {
        Installation,
        Download,
        Configuration,
        Startup,
        Shutdown,
        Plugin,
        Network,
        Permission,
        Unknown
    }

    public class PluginInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public DateTime InstalledDate { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Type { get; set; } = "Unknown";
    }

    public class ServerSettings
    {
        public string Name { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Map { get; set; } = string.Empty;
        public string GameMode { get; set; } = string.Empty;
        public string SteamAccountToken { get; set; } = string.Empty;
        public int TickRate { get; set; }
        public int MaxPlayers { get; set; }
        public bool Insecure { get; set; }
        public bool CounterStrikeSharpInstalled { get; set; }
        public string RconPassword { get; set; } = string.Empty;
        public string ServerTags { get; set; } = string.Empty;
        public bool AutoRestart { get; set; } = false;
        public DateTime CreationDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string CustomLaunchParameters { get; set; } = string.Empty;
    }


    public class ServerManager : IDisposable
    {
        private const string steamCmdUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        private const string metamodUrl = "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-2.0.0-git1345-windows.zip";
        private const string defaultCounterStrikeSharpUrl = "https://github.com/roflmuffin/CounterStrikeSharp/releases/latest/download/counterstrikesharp-with-runtime-windows.zip";

        private readonly string _baseFolder;
        private string _steamCmdPath;
        private readonly string _serverExecutableRelativePath = @"CS2ServerFiles\game\bin\win64\cs2.exe";
        private Process _currentSteamCmdProcess;
        private readonly HttpClient _httpClient;
        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        public string LastError { get; private set; } = string.Empty;
        public ServerManagerErrorType LastErrorType { get; private set; } = ServerManagerErrorType.Unknown;

        private readonly Dictionary<string, Task> _monitoringTasks = new Dictionary<string, Task>();
        private readonly Dictionary<string, bool> _monitoringCancellations = new Dictionary<string, bool>();

        public ServerManager()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _baseFolder = Path.Combine(documentsPath, "CS2ServerManager");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CS2ServerManager", "1.0.0"));
            _httpClient.Timeout = TimeSpan.FromMinutes(5);

            try
            {
                if (!Directory.Exists(_baseFolder))
                {
                    Directory.CreateDirectory(_baseFolder);
                    Logger.Info($"Base folder created: {_baseFolder}");
                }

                if (!CheckSteamInstalled())
                {
                    MessageBox.Show(
                        "Steam is not installed on this computer or was not found in the default directory. " +
                        "Steam is required to download and operate CS2 servers.",
                        "Steam not found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                string steamCmdFolder = Path.Combine(_baseFolder, "SteamCMD");
                if (!Directory.Exists(steamCmdFolder))
                {
                    Directory.CreateDirectory(steamCmdFolder);
                }
                _steamCmdPath = Path.Combine(steamCmdFolder, "steamcmd.exe");
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Configuration, "Error initializing ServerManager");
                MessageBox.Show(
                    $"Initialization error: {ex.Message}\n" +
                    "The application may not work as expected.",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            try
            {
                StopAllProcesses();
                _httpClient.Dispose();
                Logger.Info("ServerManager resources released");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error releasing resources: {ex.Message}");
            }
        }

        private void HandleError(Exception ex, ServerManagerErrorType errorType, string context)
        {
            LastError = $"{context}: {ex.Message}";
            LastErrorType = errorType;
            Logger.Error($"[{errorType}] {context}: {ex.Message}");

            if (errorType == ServerManagerErrorType.Startup ||
                errorType == ServerManagerErrorType.Installation ||
                errorType == ServerManagerErrorType.Permission)
            {
                Logger.Error(ex.ToString());
            }
        }
        private bool CheckSteamInstalled()
        {
            string[] steamPaths = {
                @"C:\Program Files (x86)\Steam\steam.exe",
                @"C:\Program Files\Steam\steam.exe"
            };

            foreach (var path in steamPaths)
            {
                if (File.Exists(path))
                {
                    Logger.Info($"Steam found at: {path}");
                    return true;
                }
            }

            Logger.Warn("Steam not found in standard directories");
            return false;
        }

        public async Task InitializeSteamCmdAsync(Action<int, string> progressCallback)
        {
            if (progressCallback == null)
            {
                progressCallback = (_, __) => { };
            }

            string steamCmdFolder = Path.Combine(_baseFolder, "SteamCMD");

            if (File.Exists(_steamCmdPath))
            {
                progressCallback(100, "SteamCMD is already installed");
                Logger.Info("SteamCMD already exists");
                return;
            }

            string tempZipPath = Path.Combine(Path.GetTempPath(), "steamcmd.zip");
            try
            {
                progressCallback(0, "Downloading SteamCMD...");
                Logger.Info("SteamCMD download started");

                var response = await _httpClient.GetAsync(steamCmdUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write,
                       FileShare.None, 8192, FileOptions.Asynchronous))
                {
                    var totalRead = 0L;
                    var buffer = new byte[8192];
                    var isMoreToRead = true;

                    do
                    {
                        var read = await contentStream.ReadAsync(buffer);
                        if (read == 0)
                        {
                            isMoreToRead = false;
                            progressCallback(90, "Download complete");
                            continue;
                        }

                        await fileStream.WriteAsync(buffer.AsMemory(0, read));
                        totalRead += read;

                        if (canReportProgress)
                        {
                            var progress = (int)((totalRead * 90.0) / totalBytes);
                            progressCallback(progress, $"SteamCMD download: {progress}%");
                        }
                    }
                    while (isMoreToRead);
                }

                progressCallback(90, "Extracting SteamCMD...");
                await Task.Run(() => ZipFile.ExtractToDirectory(tempZipPath, steamCmdFolder));

                File.Delete(tempZipPath);
                progressCallback(100, "SteamCMD successfully installed");
                Logger.Info("SteamCMD successfully downloaded and extracted");
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Download, "Error downloading SteamCMD");
                progressCallback(100, $"Error: {LastError}");

                if (File.Exists(tempZipPath))
                {
                    try { File.Delete(tempZipPath); }
                    catch { }
                }
            }
        }

        public ServerInstance CreateServer(string name, int port = 27015)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Server name cannot be empty", nameof(name));
            }

            Logger.Info($"Creating server: {name} on port {port}");

            if (port <= 0 || IsPortInUse(port))
            {
                port = FindFreePort();
                Logger.Info($"Using free port: {port}");
            }

            var instance = new ServerInstance(name)
            {
                Port = port,
                Status = "Stopped",
                CounterStrikeSharpInstalled = false,
                CreationDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            CreateServerSettings(instance);
            CreateDefaultServerConfig(name);
            CreateDefaultGamemodeConfigs(name);

            return instance;
        }

        private int FindFreePort()
        {
            for (int port = 27015; port <= 27050; port++)
            {
                if (!IsPortInUse(port))
                {
                    return port;
                }
            }

            Random random = new Random();
            int randomPort;

            do
            {
                randomPort = random.Next(10000, 65000);
            } while (IsPortInUse(randomPort));

            return randomPort;
        }

        private bool IsPortInUse(int port)
        {
            try
            {
                IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] tcpEndPoints = ipProperties.GetActiveTcpListeners();

                return tcpEndPoints.Any(endpoint => endpoint.Port == port);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error checking port {port}: {ex.Message}");
                return false;
            }
        }

        private void CreateServerSettings(ServerInstance instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance), "Server instance cannot be null");
            }

            try
            {
                var settings = new ServerSettings
                {
                    Name = instance.Name,
                    Port = instance.Port,
                    Map = instance.Map,
                    GameMode = instance.GameMode,
                    SteamAccountToken = instance.SteamAccountToken,
                    TickRate = instance.TickRate,
                    MaxPlayers = instance.MaxPlayers,
                    Insecure = instance.Insecure,
                    CounterStrikeSharpInstalled = instance.CounterStrikeSharpInstalled,
                    RconPassword = instance.RconPassword,
                    ServerTags = instance.ServerTags,
                    AutoRestart = instance.AutoRestart,
                    CreationDate = instance.CreationDate,
                    LastModifiedDate = instance.LastModifiedDate,
                    CustomLaunchParameters = instance.CustomLaunchParameters ?? string.Empty
                };

                string serverFolder = Path.Combine(_baseFolder, instance.Name);
                if (!Directory.Exists(serverFolder))
                {
                    Directory.CreateDirectory(serverFolder);
                }

                string cfgFolder = Path.Combine(serverFolder, "CS2ServerFiles", "game", "csgo", "cfg");
                if (!Directory.Exists(cfgFolder))
                {
                    Directory.CreateDirectory(cfgFolder);
                }

                string settingsPath = Path.Combine(serverFolder, "settings.json");
                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented));

                Logger.Info($"Server settings created for {instance.Name}");
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Configuration, $"Error creating server settings for {instance.Name}");
                throw;
            }
        }

        public void UpdateServerSettings(ServerInstance instance, string oldName = null)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance), "Server instance cannot be null");
            }

            try
            {
                if (!string.IsNullOrEmpty(oldName) && oldName != instance.Name)
                {
                    HandleServerRename(oldName, instance.Name);
                }

                var settings = new ServerSettings
                {
                    Name = instance.Name,
                    Port = instance.Port,
                    Map = instance.Map,
                    GameMode = instance.GameMode,
                    SteamAccountToken = instance.SteamAccountToken,
                    TickRate = instance.TickRate,
                    MaxPlayers = instance.MaxPlayers,
                    Insecure = instance.Insecure,
                    CounterStrikeSharpInstalled = instance.CounterStrikeSharpInstalled,
                    RconPassword = instance.RconPassword,
                    ServerTags = instance.ServerTags,
                    AutoRestart = instance.AutoRestart,
                    CreationDate = instance.CreationDate,
                    LastModifiedDate = DateTime.Now,
                    CustomLaunchParameters = instance.CustomLaunchParameters ?? string.Empty
                };

                string serverFolder = Path.Combine(_baseFolder, instance.Name);
                if (!Directory.Exists(serverFolder))
                {
                    Directory.CreateDirectory(serverFolder);
                }

                string settingsPath = Path.Combine(serverFolder, "settings.json");
                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented));

                Logger.Info($"Server settings updated for {instance.Name}");
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Configuration, $"Error updating settings for server {instance.Name}");
            }
        }

        private void HandleServerRename(string oldName, string newName)
        {
            string oldFolder = Path.Combine(_baseFolder, oldName);
            string newFolder = Path.Combine(_baseFolder, newName);

            if (Directory.Exists(oldFolder) && !Directory.Exists(newFolder))
            {
                Directory.Move(oldFolder, newFolder);
                Logger.Info($"Server directory renamed from {oldName} to {newName}");
            }
            else if (Directory.Exists(oldFolder) && Directory.Exists(newFolder))
            {
                Logger.Warn($"Server directory {newFolder} already exists, copying contents");

                CopyDirectoryContents(oldFolder, newFolder);

                try
                {
                    Directory.Delete(oldFolder, true);
                    Logger.Info($"Old server directory {oldFolder} deleted");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Could not delete old directory: {ex.Message}");
                }
            }
            else if (!Directory.Exists(oldFolder))
            {
                Logger.Warn($"Old server directory {oldFolder} not found");
                Directory.CreateDirectory(newFolder);
            }
        }

        private void CopyDirectoryContents(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(sourceDir.Length + 1);
                string targetPath = Path.Combine(targetDir, relativePath);
                string targetDirPath = Path.GetDirectoryName(targetPath);

                if (!Directory.Exists(targetDirPath))
                    Directory.CreateDirectory(targetDirPath);

                if (!File.Exists(targetPath) ||
                    File.GetLastWriteTime(file) > File.GetLastWriteTime(targetPath))
                {
                    File.Copy(file, targetPath, true);
                }
            }
        }

        public async Task<bool> DownloadServerFilesAsync(string serverName, Action<int, string> progressCallback)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                throw new ArgumentException("Server name cannot be empty", nameof(serverName));
            }

            if (progressCallback == null)
            {
                progressCallback = (_, __) => { };
            }

            try
            {
                if (!File.Exists(_steamCmdPath))
                {
                    progressCallback(0, "Initializing SteamCMD...");
                    await InitializeSteamCmdAsync((progress, status) => {
                        progressCallback((int)(progress * 0.2), status);
                    });
                }

                string installDir = Path.Combine(_baseFolder, serverName, "CS2ServerFiles");
                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }

                progressCallback(20, "Updating server files...");
                Logger.Info($"Server update started for {serverName}...");

                string arguments = $"+force_install_dir \"{installDir}\" +login anonymous +app_update 730 validate +quit";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _steamCmdPath,
                    Arguments = arguments,
                    WorkingDirectory = Path.GetDirectoryName(_steamCmdPath) ?? string.Empty,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    _currentSteamCmdProcess = process;
                    process.Start();

                    var outputTask = ProcessStreamAsync(process.StandardOutput, (line) => {
                        progressCallback(-1, line);
                        ParseSteamCmdProgress(line, progressCallback);
                    });

                    var errorTask = ProcessStreamAsync(process.StandardError, (line) => {
                        string errorText = $"ERROR: {line}";
                        progressCallback(-1, errorText);
                        Logger.Error(errorText);
                    });

                    progressCallback(25, "Downloading server files...");

                    await Task.WhenAll(outputTask, errorTask);
                    await process.WaitForExitAsync();
                    _currentSteamCmdProcess = null;

                    // Überprüfe Exit-Codes:
                    // - 0: Erfolg (keine Änderungen)
                    // - 7: Erfolg (Änderungen wurden angewendet)
                    // - 8: Fehler wegen nicht genügend Speicherplatz
                    if (process.ExitCode == 0 || process.ExitCode == 7)
                    {
                        progressCallback(100, "Server files successfully updated");
                        Logger.Info($"Server files for {serverName} updated successfully (Exit code: {process.ExitCode})");

                        string cfgFolder = Path.Combine(installDir, "game", "csgo", "cfg");
                        if (!Directory.Exists(cfgFolder))
                        {
                            Directory.CreateDirectory(cfgFolder);
                        }

                        CreateDefaultServerConfig(serverName);
                        CreateDefaultGamemodeConfigs(serverName);

                        var serverInstance = GetServerInstance(serverName);
                        if (serverInstance != null && serverInstance.CounterStrikeSharpInstalled)
                        {
                            try
                            {
                                progressCallback(98, "Verifying metamod configuration...");
                                await EnsureMetamodConfigurationAsync(serverInstance);
                                progressCallback(99, "Metamod configuration verified");
                            }
                            catch (Exception ex)
                            {
                                Logger.Warn($"Could not verify metamod configuration: {ex.Message}");
                            }
                        }

                        return true;
                    }
                    else if (process.ExitCode == 8)
                    {
                        string errorMessage = "Not enough disk space to download server files";
                        HandleError(
                            new Exception(errorMessage),
                            ServerManagerErrorType.Download,
                            errorMessage);
                        progressCallback(100, errorMessage);
                        return false;
                    }
                    else
                    {
                        HandleError(
                            new Exception($"SteamCMD exited with code {process.ExitCode}"),
                            ServerManagerErrorType.Download,
                            "Server file update failed");
                        progressCallback(100, "Server file update failed");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Download, "Error downloading server files");
                progressCallback(100, $"Error: {LastError}");
                return false;
            }
        }



        private ServerInstance GetServerInstance(string serverName)
        {
            try
            {
                string settingsPath = Path.Combine(_baseFolder, serverName, "settings.json");
                if (File.Exists(settingsPath))
                {
                    var settings = JsonConvert.DeserializeObject<ServerSettings>(File.ReadAllText(settingsPath));
                    if (settings != null)
                    {
                        return new ServerInstance(serverName)
                        {
                            CounterStrikeSharpInstalled = settings.CounterStrikeSharpInstalled,
                            Port = settings.Port,
                            Map = settings.Map,
                            GameMode = settings.GameMode,
                            SteamAccountToken = settings.SteamAccountToken,
                            TickRate = settings.TickRate,
                            MaxPlayers = settings.MaxPlayers,
                            Insecure = settings.Insecure,
                            RconPassword = settings.RconPassword,
                            ServerTags = settings.ServerTags,
                            AutoRestart = settings.AutoRestart,
                            CustomLaunchParameters = settings.CustomLaunchParameters
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error getting server instance for {serverName}: {ex.Message}");
            }
            return null;
        }


        private async Task ProcessStreamAsync(TextReader reader, Action<string> lineAction)
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    lineAction(line);
                }
            }
        }

        private void ParseSteamCmdProgress(string line, Action<int, string> progressCallback)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            try
            {
                if (line.Contains("Update state (0x") && line.Contains("): "))
                {
                    if (line.Contains("downloading"))
                    {
                        int percentStart = line.LastIndexOf(',') + 1;
                        if (percentStart > 0 && percentStart < line.Length)
                        {
                            string percentText = line.Substring(percentStart).Trim();
                            percentText = percentText.Replace("%", "").Trim();

                            if (int.TryParse(percentText, out int percent) && percent >= 0 && percent <= 100)
                            {
                                int scaledPercent = 25 + (percent * 65 / 100);
                                progressCallback(scaledPercent, $"Downloading server files: {percent}%");
                            }
                        }
                    }
                    else if (line.Contains("validating"))
                    {
                        progressCallback(90, "Validating server files...");
                    }
                    else if (line.Contains("committing"))
                    {
                        progressCallback(95, "Finalizing server files...");
                    }
                }
                else if (line.Contains("Success!") || line.Contains("successfully"))
                {
                    progressCallback(98, "Server files downloaded successfully");
                }
            }
            catch
            {
            }
        }
        public bool StartServer(ServerInstance instance)
        {
            if (instance == null)
            {
                HandleError(new ArgumentNullException(nameof(instance)),
                    ServerManagerErrorType.Startup, "Server instance is null");
                return false;
            }

            try
            {
                if (instance.ServerProcess != null && instance.ServerProcess.Id > 0 && !instance.ServerProcess.HasExited)
                {
                    Logger.Warn($"Start attempt for server {instance.Name} failed; Server is already running");
                    return false;
                }

                var preparationTask = instance.PrepareForStartAsync();
                preparationTask.Wait();
                var (isReady, errorMessage) = preparationTask.Result;

                if (!isReady)
                {
                    HandleError(
                        new Exception(errorMessage),
                        ServerManagerErrorType.Startup,
                        $"Server {instance.Name} is not ready to start: {errorMessage}");
                    return false;
                }

                if (instance.CounterStrikeSharpInstalled)
                {
                    try
                    {
                        var configTask = EnsureMetamodConfigurationAsync(instance);
                        configTask.Wait();

                        if (configTask.Result)
                        {
                            Logger.Info($"Verified metamod configuration in gameinfo.gi for server {instance.Name}");
                        }
                    }
                    catch (Exception configEx)
                    {
                        Logger.Warn($"Could not verify metamod configuration: {configEx.Message}");
                    }
                }

                string serverExecutablePath = ServerInstance.ServerPaths.GetServerExecutablePath(instance.Name);

                var warnings = ValidateServerSettings(instance);
                if (warnings.Count > 0)
                {
                    Logger.Warn($"Server {instance.Name} has configuration warnings: {string.Join(", ", warnings)}");
                }

                string arguments = BuildServerArguments(instance);
                Logger.Info($"Starting server with arguments: {arguments}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = serverExecutablePath,
                    Arguments = arguments,
                    WorkingDirectory = Path.GetDirectoryName(serverExecutablePath) ?? string.Empty,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                Process? process = Process.Start(startInfo);
                if (process != null)
                {
                    instance.ServerProcess = process;
                    instance.Status = "Running";
                    instance.LastStartTime = DateTime.Now;
                    Logger.Info($"Server {instance.Name} started");

                    if (instance.AutoRestart)
                    {
                        StartMonitoring(instance);
                    }

                    instance.StartStatusMonitoring();
                    return true;
                }
                else
                {
                    HandleError(
                        new Exception("Server process could not be started"),
                        ServerManagerErrorType.Startup,
                        $"Server {instance.Name} could not be started");
                    return false;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Startup, $"Error starting server {instance.Name}");
                return false;
            }
        }

        private string BuildServerArguments(ServerInstance instance)
        {
            string mapArgument = long.TryParse(instance.Map, out _)
                ? $"+host_workshop_map {instance.Map}"
                : $"+map {instance.Map}";

            string arguments = $"-dedicated -console -usercon -ip 0.0.0.0 -port {instance.Port} {mapArgument} " +
                              $"+sv_setsteamaccount {instance.SteamAccountToken} +game_type 0 +game_mode {instance.GameMode} " +
                              $"-maxplayers_override {instance.MaxPlayers} -tickrate {instance.TickRate}";

            if (!string.IsNullOrEmpty(instance.RconPassword))
            {
                arguments += $" +rcon_password \"{instance.RconPassword}\"";
            }

            if (!string.IsNullOrEmpty(instance.ServerTags))
            {
                arguments += $" +sv_tags \"{instance.ServerTags}\"";
            }

            if (!string.IsNullOrEmpty(instance.SelectedConfig))
            {
                arguments += $" +exec {instance.SelectedConfig}";
            }

            if (instance.Insecure)
            {
                arguments += " -insecure";
            }

            if (!string.IsNullOrEmpty(instance.CustomLaunchParameters))
            {
                arguments += $" {instance.CustomLaunchParameters.Trim()}";
            }

            return arguments;
        }

        private void StartMonitoring(ServerInstance instance)
        {
            if (instance == null) return;

            if (_monitoringTasks.ContainsKey(instance.Name))
            {
                _monitoringCancellations[instance.Name] = true;
                Logger.Info($"Existing monitoring for server {instance.Name} will be terminated");
            }

            _monitoringCancellations[instance.Name] = false;
            _monitoringTasks[instance.Name] = Task.Run(async () =>
            {
                Logger.Info($"Monitoring started for server {instance.Name}");

                while (!_monitoringCancellations.GetValueOrDefault(instance.Name, true))
                {
                    await Task.Delay(10000);

                    if (instance.AutoRestart && (instance.ServerProcess == null || instance.ServerProcess.HasExited))
                    {
                        Logger.Warn($"Server {instance.Name} crashed or unexpectedly terminated, restarting...");
                        StartServer(instance);
                    }
                }

                Logger.Info($"Monitoring terminated for server {instance.Name}");
            });
        }

        private void StopMonitoring(ServerInstance instance)
        {
            if (instance == null) return;

            if (_monitoringCancellations.ContainsKey(instance.Name))
            {
                _monitoringCancellations[instance.Name] = true;
                Logger.Info($"Monitoring stop requested for server {instance.Name}");
            }
        }

        public async Task<bool> StopServerAsync(ServerInstance instance)
        {
            if (instance == null)
            {
                HandleError(new ArgumentNullException(nameof(instance)),
                    ServerManagerErrorType.Shutdown, "Server instance is null");
                return false;
            }

            try
            {
                StopMonitoring(instance);
                instance.StopStatusMonitoring();

                if (instance.ServerProcess != null && !instance.ServerProcess.HasExited)
                {
                    if (!string.IsNullOrEmpty(instance.RconPassword))
                    {
                        try
                        {
                            Logger.Info($"Attempting to stop server {instance.Name} via RCON");
                            await SendRconCommandAsync(instance, "quit");

                            bool exited = await Task.Run(() =>
                                instance.ServerProcess.WaitForExit(5000));

                            if (exited)
                            {
                                instance.Status = "Stopped";
                                Logger.Info($"Server {instance.Name} properly shut down");
                                return true;
                            }
                            else
                            {
                                Logger.Warn($"Server {instance.Name} did not respond to 'quit' command");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"Server {instance.Name} could not be shut down properly: {ex.Message}");
                        }
                    }

                    Logger.Info($"Terminating server {instance.Name} via Process.Kill()");
                    instance.ServerProcess.Kill();
                    instance.Status = "Stopped";
                    Logger.Info($"Server {instance.Name} forcibly terminated");
                    return true;
                }
                else
                {
                    Logger.Info($"Server {instance.Name} is not running or has already been terminated");
                    instance.Status = "Stopped";
                    return false;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Shutdown, $"Error stopping server {instance.Name}");
                return false;
            }
        }

        public bool StopServer(ServerInstance instance)
        {
            if (instance == null) return false;

            try
            {
                StopMonitoring(instance);
                instance.StopStatusMonitoring();

                if (instance.ServerProcess != null && !instance.ServerProcess.HasExited)
                {
                    instance.ServerProcess.Kill();
                    instance.Status = "Stopped";
                    Logger.Info($"Server {instance.Name} terminated");
                    return true;
                }

                Logger.Info($"Server {instance.Name} is already not running");
                instance.Status = "Stopped";
                return false;
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Shutdown, $"Error stopping server {instance.Name}");
                return false;
            }
        }

        public void StopAllProcesses()
        {
            try
            {
                if (_currentSteamCmdProcess != null && !_currentSteamCmdProcess.HasExited)
                {
                    _currentSteamCmdProcess.Kill();
                    Logger.Info("Active SteamCMD process terminated");
                }

                foreach (var key in _monitoringCancellations.Keys.ToList())
                {
                    _monitoringCancellations[key] = true;
                }

                Task.Delay(200).Wait();

                foreach (var key in _monitoringTasks.Keys.ToList())
                {
                    if (_monitoringTasks[key].IsCompleted || _monitoringTasks[key].IsCanceled || _monitoringTasks[key].IsFaulted)
                    {
                        _monitoringTasks.Remove(key);
                    }
                }

                Logger.Info("All processes and monitoring tasks terminated");
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Shutdown, "Error terminating all processes");
            }
        }

        private async Task DownloadFileAsync(string url, string destinationPath, Action<int, string> progressCallback, string label)
        {
            if (progressCallback == null)
            {
                progressCallback = (_, __) => { };
            }

            try
            {
                string directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    bool canReportProgress = totalBytes != -1;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write,
                           FileShare.None, 8192, FileOptions.Asynchronous))
                    {
                        var totalRead = 0L;
                        var buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                        {
                            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                            totalRead += bytesRead;

                            if (canReportProgress)
                            {
                                int progress = (int)((totalRead * 100L) / totalBytes);
                                progressCallback(progress, $"Downloading {label}: {progress}%");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Download, $"Error downloading {label}");
                throw;
            }
        }

        private async Task<string> GetLatestCounterStrikeSharpUrl()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://api.github.com/repos/roflmuffin/CounterStrikeSharp/releases/latest");
                using JsonDocument json = JsonDocument.Parse(response);

                foreach (var asset in json.RootElement.GetProperty("assets").EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? string.Empty;
                    if (name.Contains("windows", StringComparison.OrdinalIgnoreCase) &&
                        name.Contains("runtime", StringComparison.OrdinalIgnoreCase))
                    {
                        string url = asset.GetProperty("browser_download_url").GetString() ?? string.Empty;
                        Logger.Info($"Found latest CounterStrikeSharp version: {name}");
                        return url;
                    }
                }

                Logger.Warn("No matching CounterStrikeSharp release asset found, falling back to default URL");
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Download, "Error determining latest CounterStrikeSharp version");
            }

            return defaultCounterStrikeSharpUrl;
        }

        public async Task<bool> InstallCounterStrikeSharpAsync(ServerInstance instance, Action<int, string> progressCallback)
        {
            if (instance == null)
            {
                HandleError(new ArgumentNullException(nameof(instance)),
                    ServerManagerErrorType.Installation, "Server instance is null");
                return false;
            }

            if (progressCallback == null)
            {
                progressCallback = (_, __) => { };
            }

            try
            {
                progressCallback(0, "Creating configuration backup...");
                await BackupServerConfigsAsync(instance);

                string serverFolder = Path.Combine(_baseFolder, instance.Name);
                string csgoAddonsPath = Path.Combine(serverFolder, "CS2ServerFiles", "game", "csgo", "addons");

                if (!Directory.Exists(csgoAddonsPath))
                {
                    Directory.CreateDirectory(csgoAddonsPath);
                }

                string metamodFolder = Path.Combine(csgoAddonsPath, "metamod");
                string metamodZipPath = Path.Combine(_baseFolder, "metamod.zip");

                if (!Directory.Exists(metamodFolder))
                {
                    progressCallback(5, "Downloading Metamod...");
                    await DownloadFileAsync(metamodUrl, metamodZipPath,
                        (progress, status) => progressCallback(5 + (progress * 25 / 100), status), "Metamod");

                    progressCallback(30, "Extracting Metamod...");

                    string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    try
                    {
                        Directory.CreateDirectory(tempDir);
                        await Task.Run(() => ZipFile.ExtractToDirectory(metamodZipPath, tempDir));

                        string tempAddonsDir = Path.Combine(tempDir, "addons");
                        if (Directory.Exists(tempAddonsDir))
                        {
                            string tempMetamodDir = Path.Combine(tempAddonsDir, "metamod");
                            if (Directory.Exists(tempMetamodDir))
                            {
                                Directory.CreateDirectory(metamodFolder);
                                CopyDirectoryContents(tempMetamodDir, metamodFolder);
                                Logger.Info($"Metamod extracted with directory correction for server {instance.Name}");
                            }
                            else
                            {
                                CopyDirectoryContents(tempAddonsDir, csgoAddonsPath);
                                Logger.Info($"Copied addons content for server {instance.Name}");
                            }
                        }
                        else
                        {
                            CopyDirectoryContents(tempDir, csgoAddonsPath);
                            Logger.Info($"Extracted metamod content directly for server {instance.Name}");
                        }
                    }
                    finally
                    {
                        await Task.Run(() => {
                            try { Directory.Delete(tempDir, true); } catch { }
                            try { File.Delete(metamodZipPath); } catch { }
                        });
                    }

                    progressCallback(35, "Metamod installed.");
                    Logger.Info($"Metamod installed for server {instance.Name}");
                }
                else
                {
                    progressCallback(35, "Metamod is already installed.");
                    Logger.Info($"Metamod for server {instance.Name} already exists");
                }

                string csSharpPath = Path.Combine(csgoAddonsPath, "counterstrikesharp");
                if (Directory.Exists(csSharpPath))
                {
                    progressCallback(40, "Creating plugin backups...");
                    string pluginsPath = Path.Combine(csSharpPath, "plugins");
                    string pluginsBackupPath = Path.Combine(_baseFolder, instance.Name, "Backups", "plugins_backup_" +
                        DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                    if (Directory.Exists(pluginsPath))
                    {
                        Directory.CreateDirectory(pluginsBackupPath);
                        CopyDirectoryContents(pluginsPath, pluginsBackupPath);
                        Logger.Info($"Plugins backed up to {pluginsBackupPath}");
                    }
                }

                progressCallback(45, "Preparing CounterStrikeSharp...");
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"css_{DateTime.Now.Ticks}.zip");

                try
                {
                    progressCallback(50, "Extracting CounterStrikeSharp resource...");
                    using (Stream resourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CS2ServerManager.Resources.css.zip"))
                    {
                        if (resourceStream == null)
                        {
                            Logger.Error("CounterStrikeSharp ZIP resource not found in assembly");
                            throw new FileNotFoundException("CounterStrikeSharp ZIP resource could not be found");
                        }

                        using (FileStream fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write))
                        {
                            await resourceStream.CopyToAsync(fileStream);
                        }
                    }

                    progressCallback(70, "Installing CounterStrikeSharp...");

                    string pluginsBackupDir = Directory.GetDirectories(Path.Combine(_baseFolder, instance.Name, "Backups"), "plugins_backup_*")
                        .OrderByDescending(d => d)
                        .FirstOrDefault() ?? string.Empty;

                    await Task.Run(() => ZipFile.ExtractToDirectory(tempZipPath, csgoAddonsPath, true));
                    Logger.Info($"CounterStrikeSharp extracted directly to addons folder for server {instance.Name}");

                    string newPluginsDir = Path.Combine(csgoAddonsPath, "counterstrikesharp", "plugins");
                    Directory.CreateDirectory(newPluginsDir);

                    if (!string.IsNullOrEmpty(pluginsBackupDir) && Directory.Exists(pluginsBackupDir))
                    {
                        progressCallback(90, "Restoring plugins...");
                        CopyDirectoryContents(pluginsBackupDir, newPluginsDir);
                        Logger.Info("Restored plugins from backup");
                    }
                }
                finally
                {
                    if (File.Exists(tempZipPath))
                    {
                        try { File.Delete(tempZipPath); }
                        catch (Exception ex)
                        {
                            Logger.Warn($"Could not delete temporary CSS ZIP file: {ex.Message}");
                        }
                    }
                }

                string gameInfoPath = Path.Combine(serverFolder, "CS2ServerFiles", "game", "csgo", "gameinfo.gi");
                if (File.Exists(gameInfoPath))
                {
                    progressCallback(95, "Updating gameinfo.gi...");
                    string content = File.ReadAllText(gameInfoPath);

                    if (!content.Contains("Game csgo/addons/metamod"))
                    {
                        string newContent = content.Replace("Game_LowViolence csgo_lv",
                            "Game_LowViolence csgo_lv\n\t\tGame csgo/addons/metamod");
                        File.WriteAllText(gameInfoPath, newContent);
                        Logger.Info("Updated gameinfo.gi for metamod");
                    }

                    if (!content.Contains("Game csgo/addons/counterstrikesharp/core"))
                    {
                        content = File.ReadAllText(gameInfoPath); 
                        string newContent = content.Replace("Game csgo/addons/metamod",
                            "Game csgo/addons/metamod\n\t\tGame csgo/addons/counterstrikesharp/core");
                        File.WriteAllText(gameInfoPath, newContent);
                        Logger.Info("Updated gameinfo.gi for CounterStrikeSharp");
                    }
                }

                progressCallback(100, "CounterStrikeSharp successfully installed.");
                Logger.Info($"CounterStrikeSharp installed for server {instance.Name}");

                instance.CounterStrikeSharpInstalled = true;
                instance.LastModifiedDate = DateTime.Now;
                UpdateServerSettings(instance);

                return true;
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Installation, "Error installing CounterStrikeSharp");
                progressCallback(100, $"Installation failed: {ex.Message}");
                return false;
            }
        }


        public List<string> ValidateServerSettings(ServerInstance instance)
        {
            if (instance == null)
                return new List<string> { "Server instance is null" };

            var warnings = new List<string>();

            if (instance.Port < 1024 || instance.Port > 65535)
                warnings.Add("Port should be between 1024 and 65535");

            if (IsPortInUse(instance.Port))
                warnings.Add($"Port {instance.Port} is already in use");

            if (string.IsNullOrEmpty(instance.SteamAccountToken) || instance.SteamAccountToken == "YOURLOGINTOKEN")
                warnings.Add("A valid Steam Account Token is required for public servers");

            if (instance.MaxPlayers < 1 || instance.MaxPlayers > 64)
                warnings.Add("Player count should be between 1 and 64");

            if (string.IsNullOrEmpty(instance.Map))
                warnings.Add("A valid map name or workshop ID is required");

            if (string.IsNullOrEmpty(instance.RconPassword))
                warnings.Add("Without an RCON password, remote server management is not possible");

            return warnings;
        }

        public async Task<bool> DownloadWorkshopItemAsync(ServerInstance instance, string workshopId, Action<int, string> progressCallback)
        {
            if (instance == null || string.IsNullOrWhiteSpace(workshopId))
            {
                HandleError(new ArgumentException("Invalid server instance or workshop ID"),
                    ServerManagerErrorType.Download, "Invalid parameters for workshop download");
                return false;
            }

            if (progressCallback == null)
            {
                progressCallback = (_, __) => { };
            }

            try
            {
                if (!long.TryParse(workshopId, out _))
                {
                    HandleError(new ArgumentException("Workshop ID must be a number"),
                        ServerManagerErrorType.Download, "Invalid workshop ID format");
                    progressCallback(0, "Error: Workshop ID must be a number");
                    return false;
                }

                if (!File.Exists(_steamCmdPath))
                {
                    progressCallback(0, "Initializing SteamCMD...");
                    await InitializeSteamCmdAsync((progress, status) => {
                        progressCallback((int)(progress * 0.2), status);
                    });
                }

                string installDir = Path.Combine(_baseFolder, instance.Name, "CS2ServerFiles");
                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }

                progressCallback(20, $"Downloading workshop item {workshopId}...");
                Logger.Info($"Started download of workshop item {workshopId} for server {instance.Name}");

                string arguments = $"+force_install_dir \"{installDir}\" +login anonymous +workshop_download_item 730 {workshopId} validate +quit";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _steamCmdPath,
                    Arguments = arguments,
                    WorkingDirectory = Path.GetDirectoryName(_steamCmdPath) ?? string.Empty,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    _currentSteamCmdProcess = process;
                    process.Start();

                    var outputTask = ProcessStreamAsync(process.StandardOutput, (line) => {
                        progressCallback(-1, line);
                        ParseSteamCmdProgress(line, progressCallback);
                    });

                    var errorTask = ProcessStreamAsync(process.StandardError, (line) => {
                        progressCallback(-1, "ERROR: " + line);
                        Logger.Error($"SteamCMD error: {line}");
                    });

                    await Task.WhenAll(outputTask, errorTask);
                    await process.WaitForExitAsync();
                    _currentSteamCmdProcess = null;

                    if (process.ExitCode == 0)
                    {
                        progressCallback(100, "Workshop map downloaded successfully");
                        Logger.Info($"Workshop item {workshopId} downloaded for server {instance.Name}");

                        CopyWorkshopMapToMapsFolder(instance, workshopId);

                        return true;
                    }
                    else
                    {
                        HandleError(
                            new Exception($"SteamCMD exited with code {process.ExitCode}"),
                            ServerManagerErrorType.Download,
                            $"Workshop item {workshopId} could not be downloaded");
                        progressCallback(100, "Workshop map download failed");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Download, $"Error downloading workshop item {workshopId}");
                progressCallback(100, $"Error: {LastError}");
                return false;
            }
        }

        private void CopyWorkshopMapToMapsFolder(ServerInstance instance, string workshopId)
        {
            try
            {
                string workshopDir = Path.Combine(
                    _baseFolder,
                    instance.Name,
                    "CS2ServerFiles",
                    "steamapps",
                    "workshop",
                    "content",
                    "730",
                    workshopId);

                string mapsDir = Path.Combine(
                    _baseFolder,
                    instance.Name,
                    "CS2ServerFiles",
                    "game",
                    "csgo",
                    "maps");

                if (!Directory.Exists(mapsDir))
                {
                    Directory.CreateDirectory(mapsDir);
                }

                if (Directory.Exists(workshopDir))
                {
                    int copiedFiles = 0;
                    var bspFiles = Directory.GetFiles(workshopDir, "*.bsp", SearchOption.AllDirectories);

                    foreach (var bspFile in bspFiles)
                    {
                        string fileName = Path.GetFileName(bspFile);
                        string destPath = Path.Combine(mapsDir, fileName);

                        if (!File.Exists(destPath) ||
                            File.GetLastWriteTime(bspFile) > File.GetLastWriteTime(destPath))
                        {
                            File.Copy(bspFile, destPath, true);
                            copiedFiles++;
                            Logger.Info($"Copied map {fileName} to maps folder");
                        }
                    }

                    if (copiedFiles > 0)
                    {
                        Logger.Info($"{copiedFiles} map(s) were copied to the maps folder");
                    }
                    else if (bspFiles.Length == 0)
                    {
                        Logger.Warn($"No BSP files found in workshop item {workshopId}");
                    }
                }
                else
                {
                    Logger.Warn($"Workshop directory for item {workshopId} not found");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error copying workshop map: {ex.Message}");
            }
        }

        public async Task<bool> EnsureMetamodConfigurationAsync(ServerInstance instance)
        {
            if (instance == null)
            {
                HandleError(new ArgumentNullException(nameof(instance)),
                    ServerManagerErrorType.Configuration, "Server instance is null");
                return false;
            }

            try
            {
                string gameInfoPath = ServerInstance.ServerPaths.GetGameInfoPath(instance.Name);

                if (!File.Exists(gameInfoPath))
                {
                    Logger.Warn($"gameinfo.gi not found for server {instance.Name}");
                    return false;
                }

                string content = await File.ReadAllTextAsync(gameInfoPath);
                bool needsUpdate = !content.Contains("Game csgo/addons/metamod");

                if (needsUpdate)
                {
                    string backupPath = Path.Combine(
                        ServerInstance.ServerPaths.GetServerFolder(instance.Name),
                        "Backups",
                        $"gameinfo_backup_{DateTime.Now:yyyyMMdd_HHmmss}.gi");

                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                    File.Copy(gameInfoPath, backupPath, true);

                    string searchPathsPattern = @"(SearchPaths\s*\{[^\{]*?)(Game\s+[\w\/\\\.\s]+)";
                    string metamodEntry = "Game\tcsgo/addons/metamod\n\t\t";

                    var match = Regex.Match(content, searchPathsPattern);
                    if (match.Success)
                    {
                        content = Regex.Replace(content, searchPathsPattern,
                            m => $"{m.Groups[1].Value}{metamodEntry}{m.Groups[2].Value}");

                        await File.WriteAllTextAsync(gameInfoPath, content);
                        Logger.Info($"Updated gameinfo.gi for server {instance.Name} with regex method");
                        return true;
                    }
                    else
                    {
                        string newContent = content.Replace(
                            "Game_LowViolence csgo_lv",
                            "Game_LowViolence csgo_lv\n\t\tGame csgo/addons/metamod");

                        await File.WriteAllTextAsync(gameInfoPath, newContent);
                        Logger.Info($"Updated gameinfo.gi for server {instance.Name} with simple method");
                        return true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Configuration,
                    $"Error updating gameinfo.gi for server {instance.Name}");
                return false;
            }
        }

        public async Task<string> BackupServerConfigsAsync(ServerInstance instance)
        {
            if (instance == null)
            {
                HandleError(new ArgumentNullException(nameof(instance)),
                    ServerManagerErrorType.Configuration, "Server instance is null");
                return string.Empty;
            }

            try
            {
                string serverFolder = Path.Combine(_baseFolder, instance.Name);
                string cfgFolder = Path.Combine(serverFolder, "CS2ServerFiles", "game", "csgo", "cfg");

                if (!Directory.Exists(cfgFolder))
                {
                    Logger.Warn($"Configuration folder for {instance.Name} not found");
                    return string.Empty;
                }

                string backupFolder = Path.Combine(serverFolder, "Backups");
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string backupFileName = $"{instance.Name}_config_backup_{timestamp}.zip";
                string backupPath = Path.Combine(backupFolder, backupFileName);

                string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);

                try
                {
                    foreach (string filePath in Directory.GetFiles(cfgFolder))
                    {
                        string fileName = Path.GetFileName(filePath);
                        string destFile = Path.Combine(tempDir, fileName);
                        File.Copy(filePath, destFile);
                    }

                    await Task.Run(() => ZipFile.CreateFromDirectory(tempDir, backupPath));

                    await CleanupOldBackups(backupFolder, 10);
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }

                Logger.Info($"Configuration backup created for {instance.Name}: {backupPath}");
                return backupPath;
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Configuration, "Error creating configuration backup");
                return string.Empty;
            }
        }

        private async Task CleanupOldBackups(string backupFolder, int keepCount)
        {
            try
            {
                await Task.Run(() => {
                    var backups = new DirectoryInfo(backupFolder)
                        .GetFiles("*.zip")
                        .OrderByDescending(f => f.LastWriteTime)
                        .Skip(keepCount);

                    foreach (var oldBackup in backups)
                    {
                        try
                        {
                            oldBackup.Delete();
                            Logger.Info($"Old backup deleted: {oldBackup.Name}");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"Could not delete old backup {oldBackup.Name}: {ex.Message}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error cleaning up old backups: {ex.Message}");
            }
        }

        public async Task<string> SendRconCommandAsync(ServerInstance instance, string command)
        {
            if (instance == null || string.IsNullOrWhiteSpace(command))
            {
                LastError = "Invalid parameters for RCON command";
                LastErrorType = ServerManagerErrorType.Configuration;
                return string.Empty;
            }

            if (string.IsNullOrEmpty(instance.RconPassword))
            {
                LastError = "RCON password is not configured";
                LastErrorType = ServerManagerErrorType.Configuration;
                return string.Empty;
            }

            if (!instance.IsRunning)
            {
                LastError = "Server is not running";
                LastErrorType = ServerManagerErrorType.Startup;
                return string.Empty;
            }

            try
            {
                await Task.Delay(100);

                Logger.Info($"RCON command sent to server {instance.Name}: {command}");
                return $"Command '{command}' sent successfully (RCON implementation required)";
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Network, $"Error sending RCON command to {instance.Name}");
                return $"RCON error: {ex.Message}";
            }
        }

        public bool CreateDefaultServerConfig(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                HandleError(new ArgumentException("Server name cannot be empty"),
                    ServerManagerErrorType.Configuration, "Invalid server name");
                return false;
            }

            try
            {
                string cfgFolder = Path.Combine(_baseFolder, serverName, "CS2ServerFiles", "game", "csgo", "cfg");
                if (!Directory.Exists(cfgFolder))
                {
                    Directory.CreateDirectory(cfgFolder);
                }

                string configPath = Path.Combine(cfgFolder, "server.cfg");
                if (!File.Exists(configPath))
                {
                    string defaultConfig =
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
                        "mp_match_end_restart 1\r\n" +
                        "sv_hibernate_when_empty 0\r\n" +
                        "sv_hibernate_ms 0\r\n";

                    File.WriteAllText(configPath, defaultConfig);
                    Logger.Info($"Default server configuration created for {serverName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Configuration, $"Error creating default server configuration for {serverName}");
                return false;
            }
        }

        public bool CreateDefaultGamemodeConfigs(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName))
                return false;

            try
            {
                string cfgFolder = Path.Combine(_baseFolder, serverName, "CS2ServerFiles", "game", "csgo", "cfg");
                if (!Directory.Exists(cfgFolder))
                {
                    Directory.CreateDirectory(cfgFolder);
                }

                bool created = false;

                string fiveVfiveConfigPath = Path.Combine(cfgFolder, "5v5.cfg");
                if (!File.Exists(fiveVfiveConfigPath))
                {
                    string config =
                        "game_mode 1\r\n" +
                        "game_type 0\r\n" +
                        "mp_maxrounds 30\r\n" +
                        "mp_startmoney 800\r\n" +
                        "mp_freezetime 15\r\n" +
                        "mp_roundtime 1.92\r\n" +
                        "mp_round_restart_delay 5\r\n" +
                        "mp_afterroundmoney 0\r\n" +
                        "mp_playercashawards 1\r\n" +
                        "mp_teamcashawards 1\r\n" +
                        "mp_maxmoney 16000\r\n" +
                        "mp_buytime 20\r\n" +
                        "mp_buy_anywhere 0\r\n" +
                        "mp_c4timer 40\r\n" +
                        "mp_halftime 1\r\n" +
                        "mp_friendlyfire 1\r\n" +
                        "mp_autokick 1\r\n" +
                        "mp_autoteambalance 0\r\n" +
                        "mp_limitteams 0\r\n" +
                        "sv_alltalk 0\r\n" +
                        "sv_deadtalk 0\r\n" +
                        "sv_full_alltalk 0\r\n" +
                        "mp_warmuptime 60\r\n" +
                        "mp_match_end_restart 1\r\n" +
                        "mp_overtime_enable 1\r\n" +
                        "mp_overtime_maxrounds 6\r\n";

                    File.WriteAllText(fiveVfiveConfigPath, config);
                    Logger.Info($"5v5 configuration created for {serverName}");
                    created = true;
                }

                string twoVtwoConfigPath = Path.Combine(cfgFolder, "2v2.cfg");
                if (!File.Exists(twoVtwoConfigPath))
                {
                    string config =
                        "game_mode 2\r\n" +
                        "game_type 0\r\n" +
                        "mp_maxrounds 16\r\n" +
                        "mp_startmoney 800\r\n" +
                        "mp_freezetime 10\r\n" +
                        "mp_roundtime 1.92\r\n" +
                        "mp_round_restart_delay 5\r\n" +
                        "mp_afterroundmoney 0\r\n" +
                        "mp_playercashawards 1\r\n" +
                        "mp_teamcashawards 1\r\n" +
                        "mp_maxmoney 16000\r\n" +
                        "mp_buytime 20\r\n" +
                        "mp_buy_anywhere 0\r\n" +
                        "mp_c4timer 40\r\n" +
                        "mp_halftime 0\r\n" +
                        "mp_friendlyfire 1\r\n" +
                        "mp_autokick 1\r\n" +
                        "mp_autoteambalance 0\r\n" +
                        "mp_limitteams 0\r\n" +
                        "sv_alltalk 0\r\n" +
                        "sv_deadtalk 0\r\n" +
                        "sv_full_alltalk 0\r\n" +
                        "mp_warmuptime 60\r\n" +
                        "mp_match_end_restart 1\r\n";

                    File.WriteAllText(twoVtwoConfigPath, config);
                    Logger.Info($"2v2 configuration created for {serverName}");
                    created = true;
                }

                string deathmatchConfigPath = Path.Combine(cfgFolder, "deathmatch.cfg");
                if (!File.Exists(deathmatchConfigPath))
                {
                    string config =
                        "game_mode 2\r\n" +
                        "game_type 1\r\n" +
                        "mp_timelimit 10\r\n" +
                        "mp_fraglimit 0\r\n" +
                        "mp_maxrounds 0\r\n" +
                        "mp_roundtime 10\r\n" +
                        "mp_freezetime 0\r\n" +
                        "mp_buytime 9999\r\n" +
                        "mp_buy_anywhere 1\r\n" +
                        "mp_startmoney 16000\r\n" +
                        "mp_respawn_on_death_t 1\r\n" +
                        "mp_respawn_on_death_ct 1\r\n" +
                        "mp_respawn_immunitytime 0\r\n" +
                        "mp_autokick 0\r\n" +
                        "mp_autoteambalance 0\r\n" +
                        "mp_limitteams 0\r\n" +
                        "mp_warmuptime 0\r\n" +
                        "sv_infinite_ammo 1\r\n" +
                        "mp_death_drop_gun 0\r\n" +
                        "mp_death_drop_grenade 0\r\n" +
                        "mp_death_drop_defuser 0\r\n" +
                        "sv_full_alltalk 1\r\n" +
                        "sv_deadtalk 1\r\n";

                    File.WriteAllText(deathmatchConfigPath, config);
                    Logger.Info($"Deathmatch configuration created for {serverName}");
                    created = true;
                }

                string pracConfigPath = Path.Combine(cfgFolder, "prac.cfg");
                if (!File.Exists(pracConfigPath))
                {
                    string config =
                        "sv_cheats 1\r\n" +
                        "mp_limitteams 0\r\n" +
                        "mp_autoteambalance 0\r\n" +
                        "mp_maxmoney 60000\r\n" +
                        "mp_startmoney 60000\r\n" +
                        "mp_freezetime 0\r\n" +
                        "mp_buytime 9999\r\n" +
                        "mp_buy_anywhere 1\r\n" +
                        "sv_grenade_trajectory 1\r\n" +
                        "sv_grenade_trajectory_time 15\r\n" +
                        "sv_showimpacts 1\r\n" +
                        "sv_showimpacts_time 10\r\n" +
                        "sv_infinite_ammo 2\r\n" +
                        "mp_roundtime 60\r\n" +
                        "mp_roundtime_defuse 60\r\n" +
                        "mp_warmup_end\r\n" +
                        "mp_restartgame 1\r\n";

                    File.WriteAllText(pracConfigPath, config);
                    Logger.Info($"Practice configuration created for {serverName}");
                    created = true;
                }

                return created;
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Configuration, $"Error creating gamemode configurations for {serverName}");
                return false;
            }
        }

        public List<PluginInfo> GetInstalledPlugins(ServerInstance instance)
        {
            List<PluginInfo> plugins = new List<PluginInfo>();

            if (instance == null) return plugins;

            try
            {
                string cssPluginsDir = Path.Combine(
                    _baseFolder,
                    instance.Name,
                    "CS2ServerFiles",
                    "game",
                    "csgo",
                    "addons",
                    "counterstrikesharp",
                    "plugins");

                if (Directory.Exists(cssPluginsDir))
                {
                    foreach (var dir in Directory.GetDirectories(cssPluginsDir))
                    {
                        string pluginName = new DirectoryInfo(dir).Name;
                        var pluginInfo = new PluginInfo
                        {
                            Name = pluginName,
                            FilePath = dir,
                            InstalledDate = Directory.GetCreationTime(dir),
                            Type = "CounterStrikeSharp Plugin"
                        };

                        string manifestPath = Path.Combine(dir, "plugin.json");
                        if (File.Exists(manifestPath))
                        {
                            try
                            {
                                string jsonContent = File.ReadAllText(manifestPath);
                                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                                {
                                    var root = doc.RootElement;

                                    if (root.TryGetProperty("name", out var name))
                                        pluginInfo.Name = name.GetString() ?? pluginName;

                                    if (root.TryGetProperty("version", out var version))
                                        pluginInfo.Version = version.GetString() ?? "1.0.0";

                                    if (root.TryGetProperty("author", out var author))
                                        pluginInfo.Author = author.GetString() ?? "Unknown";

                                    if (root.TryGetProperty("description", out var description))
                                        pluginInfo.Description = description.GetString() ?? string.Empty;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Warn($"Error parsing plugin metadata for {pluginName}: {ex.Message}");
                            }
                        }

                        var dllFiles = Directory.GetFiles(dir, "*.dll");
                        if (dllFiles.Length > 0)
                        {
                            foreach (var dll in dllFiles)
                            {
                                var dllPlugin = new PluginInfo
                                {
                                    Name = Path.GetFileNameWithoutExtension(dll),
                                    FilePath = dir,
                                    FileName = Path.GetFileName(dll),
                                    InstalledDate = File.GetCreationTime(dll),
                                    Type = "Compiled (.dll)",
                                    Version = pluginInfo.Version,
                                    Author = pluginInfo.Author,
                                    Description = pluginInfo.Description
                                };
                                plugins.Add(dllPlugin);
                            }
                        }
                        else
                        {
                            var csFiles = Directory.GetFiles(dir, "*.cs");
                            if (csFiles.Length > 0)
                            {
                                foreach (var cs in csFiles)
                                {
                                    var csPlugin = new PluginInfo
                                    {
                                        Name = Path.GetFileNameWithoutExtension(cs),
                                        FilePath = dir,
                                        FileName = Path.GetFileName(cs),
                                        InstalledDate = File.GetCreationTime(cs),
                                        Type = "Script (.cs)",
                                        Version = pluginInfo.Version,
                                        Author = pluginInfo.Author,
                                        Description = pluginInfo.Description
                                    };
                                    plugins.Add(csPlugin);
                                }
                            }
                            else
                            {
                                plugins.Add(pluginInfo);
                            }
                        }
                    }
                }
                else
                {
                    string addonsDir = Path.Combine(
                        _baseFolder,
                        instance.Name,
                        "CS2ServerFiles",
                        "game",
                        "csgo",
                        "addons");

                    if (Directory.Exists(addonsDir))
                    {
                        foreach (var dir in Directory.GetDirectories(addonsDir))
                        {
                            if (Path.GetFileName(dir).ToLower() != "metamod")
                            {
                                var pluginInfo = new PluginInfo
                                {
                                    Name = new DirectoryInfo(dir).Name,
                                    FilePath = dir,
                                    InstalledDate = Directory.GetCreationTime(dir),
                                    Type = "Addon Directory"
                                };
                                plugins.Add(pluginInfo);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting installed plugins: {ex.Message}");
            }

            return plugins;
        }

        public async Task<Dictionary<string, string>> GetServerStatsAsync(ServerInstance instance)
        {
            Dictionary<string, string> stats = new Dictionary<string, string>();

            if (instance == null || !instance.IsRunning)
                return stats;

            try
            {
                stats["Uptime"] = instance.Uptime.ToString(@"hh\:mm\:ss");
                stats["ProcessId"] = instance.ServerProcess.Id.ToString();

                await Task.Run(() => {
                    try
                    {
                        var process = Process.GetProcessById(instance.ServerProcess.Id);
                        stats["CPU"] = $"{process.TotalProcessorTime.TotalSeconds:F1}s";
                        stats["Memory"] = $"{process.WorkingSet64 / 1024 / 1024} MB";
                        stats["Threads"] = process.Threads.Count.ToString();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Error collecting process statistics: {ex.Message}");
                    }
                });

                if (!string.IsNullOrEmpty(instance.RconPassword))
                {
                    try
                    {
                        string status = await SendRconCommandAsync(instance, "status");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Error retrieving player statistics via RCON: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Unknown, "Error retrieving server statistics");
            }

            return stats;
        }

        public async Task<bool> IsServerRunningAsync(ServerInstance instance)
        {
            try
            {
                if (instance?.ServerProcess == null) return false;

                bool exited = await Task.Run(() => instance.ServerProcess.HasExited);
                return !exited;
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Unknown, $"Error checking status of server {instance?.Name}");
                return false;
            }
        }

        public async Task<bool> WaitForServerStartAsync(ServerInstance instance, int timeoutSeconds = 30)
        {
            try
            {
                if (instance?.ServerProcess == null) return false;

                int attempts = 0;
                while (attempts < timeoutSeconds && !instance.ServerProcess.HasExited)
                {
                    await Task.Delay(1000);
                    attempts++;
                }

                return !instance.ServerProcess.HasExited;
            }
            catch (Exception ex)
            {
                HandleError(ex, ServerManagerErrorType.Startup, $"Error waiting for server start {instance?.Name}");
                return false;
            }
        }
    }
}
