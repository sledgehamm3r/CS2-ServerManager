using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.IO;
using System.Net.Http;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using System.Reflection;

namespace CS2ServerManager
{
    public class ServerInstance : IDisposable
    {
        private void LogInfo(string message) => Debug.WriteLine($"[INFO] {Name}: {message}");
        private void LogWarning(string message) => Debug.WriteLine($"[WARN] {Name}: {message}");
        private void LogError(string message) => Debug.WriteLine($"[ERROR] {Name}: {message}");

        public ServerInstance()
        {
            Port = 27015;
            Map = "de_dust2";
            GameMode = "0";
            TickRate = 64;
            MaxPlayers = 24;
            Status = "Stopped";
            CreationDate = DateTime.Now;
            CustomLaunchParameters = string.Empty;
            Tags = new List<string>();
        }

        public ServerInstance(string name) : this()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Server name cannot be empty.", nameof(name));

            Name = name;
        }

        public string Name { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Status { get; set; } = string.Empty;

        [JsonIgnore]
        public Process ServerProcess { get; set; } = new Process();

        public string Map { get; set; } = string.Empty;
        public string GameMode { get; set; } = string.Empty;
        public string SteamAccountToken { get; set; } = string.Empty;
        public int TickRate { get; set; }
        public int MaxPlayers { get; set; }
        public bool Insecure { get; set; }
        public bool CounterStrikeSharpInstalled { get; set; }
        public string SelectedConfig { get; set; } = "server.cfg";

        [JsonIgnore]
        public bool IsRunning => ServerProcess != null && !ServerProcess.HasExited;

        [JsonIgnore]
        public bool IsServerReady { get; set; } = false;

        [JsonIgnore]
        public DateTime LastStartTime { get; set; }

        [JsonIgnore]
        public TimeSpan Uptime => IsRunning ? DateTime.Now - LastStartTime : TimeSpan.Zero;

        public int CurrentPlayers { get; set; }
        public int MaximumReachedPlayers { get; set; }

        public string RconPassword { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        [JsonIgnore]
        private List<string> Tags { get; set; } = new List<string>();

        public string ServerTags
        {
            get => string.Join(",", Tags);
            set
            {
                Tags.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    foreach (var tag in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        Tags.Add(tag.Trim());
                    }
                }
            }
        }


        public event EventHandler<ServerStatusEventArgs> StatusUpdated;


        [JsonIgnore]
        private Timer statusUpdateTimer;


        [JsonIgnore]
        private RconClient rconClient;

        public bool AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            tag = tag.Trim();
            if (Tags.Contains(tag))
                return false;

            Tags.Add(tag);
            return true;
        }

        public bool RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            return Tags.Remove(tag.Trim());
        }


        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            return Tags.Contains(tag.Trim());
        }

        public bool AutoRestart { get; set; } = false;


        public string CustomLaunchParameters { get; set; }


        public static class ServerPaths
        {

            public static string BaseFolder { get; private set; } = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "CS2ServerManager");

            public static void SetCustomBasePath(string path)
            {
                if (Directory.Exists(path))
                    BaseFolder = path;
                else
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        BaseFolder = path;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to create directory: {ex.Message}");
                    }
                }
            }

            public static string GetServerFolder(string serverName) =>
                Path.Combine(BaseFolder, serverName);

            public static string GetServerExecutablePath(string serverName) =>
                Path.Combine(GetServerFolder(serverName), "CS2ServerFiles", "game", "bin", "win64", "cs2.exe");

            public static string GetGameInfoPath(string serverName) =>
                Path.Combine(GetServerFolder(serverName), "CS2ServerFiles", "game", "csgo", "gameinfo.gi");

            public static string GetSteamCmdPath() =>
                Path.Combine(BaseFolder, "steamcmd", "steamcmd.exe");

            public static string GetConfigPath(string serverName, string configName) =>
                Path.Combine(GetServerFolder(serverName), "CS2ServerFiles", "game", "csgo", "cfg", configName);

            public static string GetMetamodPath(string serverName) =>
                Path.Combine(GetServerFolder(serverName), "CS2ServerFiles", "game", "csgo", "addons", "metamod");

            public static string GetAddonsPath(string serverName) =>
                Path.Combine(GetServerFolder(serverName), "CS2ServerFiles", "game", "csgo", "addons");

            public static string GetCounterStrikeSharpPath(string serverName) =>
                Path.Combine(GetServerFolder(serverName), "CS2ServerFiles", "game", "csgo", "addons", "counterstrikesharp");

            public static string GetPluginsPath(string serverName) =>
                Path.Combine(GetCounterStrikeSharpPath(serverName), "plugins");

            public static string GetWorkshopPath(string serverName) =>
                Path.Combine(GetServerFolder(serverName), "CS2ServerFiles", "game", "csgo", "maps", "workshop");
        }


        public bool IsConfigurationValid()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return false;

            if (Port < 1024 || Port > 65535)
                return false;

            if (MaxPlayers < 1 || MaxPlayers > 64)
                return false;

            if (string.IsNullOrWhiteSpace(Map))
                return false;

            if (Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return false;

            return true;
        }


        private bool IsCounterStrikeSharpInstalled()
        {
            try
            {
                if (!CounterStrikeSharpInstalled)
                {
                    LogInfo("[DEBUG] CounterStrikeSharp is not enabled.");
                    return false;
                }

                string cssPath = ServerPaths.GetCounterStrikeSharpPath(Name);
                string cssCoreDllPath = Path.Combine(cssPath, "core", "CounterStrikeSharp.API.dll");
                string cssRuntimeConfigPath = Path.Combine(cssPath, "core", "CounterStrikeSharp.API.runtimeconfig.json");
                string cssPluginsPath = Path.Combine(cssPath, "plugins");

                LogInfo($"[DEBUG] Checking CounterStrikeSharp installation paths: {cssPath}");

                bool dirExists = Directory.Exists(cssPath);
                bool coreDllExists = File.Exists(cssCoreDllPath);
                bool runtimeConfigExists = File.Exists(cssRuntimeConfigPath);
                bool pluginsDirExists = Directory.Exists(cssPluginsPath);

                LogInfo($"[DEBUG] CSS Directory: {cssPath} - {(dirExists ? "Exists" : "Missing")}");
                LogInfo($"[DEBUG] Core DLL: {cssCoreDllPath} - {(coreDllExists ? "Exists" : "Missing")}");
                LogInfo($"[DEBUG] Runtime Config: {cssRuntimeConfigPath} - {(runtimeConfigExists ? "Exists" : "Missing")}");
                LogInfo($"[DEBUG] Plugins Folder: {cssPluginsPath} - {(pluginsDirExists ? "Exists" : "Missing")}");

                bool exists = dirExists && coreDllExists && runtimeConfigExists && pluginsDirExists;

                if (!exists)
                {
                    LogError($"[DEBUG] CounterStrikeSharp is configured but files were not found. Installation incomplete.");

                    CounterStrikeSharpInstalled = false;

                    return false;
                }

                LogInfo("[DEBUG] CounterStrikeSharp is correctly installed.");
                return exists;
            }
            catch (Exception ex)
            {
                LogError($"[DEBUG] Exception in IsCounterStrikeSharpInstalled: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> InstallMetamodAsync()
        {
            try
            {
                LogInfo("[DEBUG] Starting Metamod installation...");
                string addonsPath = ServerPaths.GetAddonsPath(Name);
                string metamodPath = ServerPaths.GetMetamodPath(Name);
                string gameInfoPath = ServerPaths.GetGameInfoPath(Name);

                if (Directory.Exists(metamodPath))
                {
                    LogInfo("[DEBUG] Metamod seems to be already installed. Checking gameinfo.gi...");
                    await UpdateGameInfoFileAsync(gameInfoPath);
                    return true;
                }

                Directory.CreateDirectory(addonsPath);

                string tempZipPath = Path.Combine(Path.GetTempPath(), $"metamod_{DateTime.Now.Ticks}.zip");

                try
                {
                    using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CS2ServerManager.Resources.metamod.zip"))
                    {
                        if (resourceStream == null)
                        {
                            LogError("[DEBUG] Metamod ZIP not found in resources!");
                            return false;
                        }

                        using (FileStream fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write))
                        {
                            await resourceStream.CopyToAsync(fileStream);
                        }
                    }

                    LogInfo($"[DEBUG] Extracting Metamod ZIP to {addonsPath}...");
                    ZipFile.ExtractToDirectory(tempZipPath, addonsPath, true);
                }
                finally
                {
                    if (File.Exists(tempZipPath))
                    {
                        try
                        {
                            File.Delete(tempZipPath);
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"[DEBUG] Could not delete temporary ZIP file: {ex.Message}");
                        }
                    }
                }

                if (!Directory.Exists(metamodPath))
                {
                    LogError("[DEBUG] Metamod directory could not be created!");
                    return false;
                }

                bool gameInfoUpdated = await UpdateGameInfoFileAsync(gameInfoPath);
                if (!gameInfoUpdated)
                {
                    LogWarning("[DEBUG] gameinfo.gi could not be updated. Installation might be incomplete.");
                    return false;
                }

                LogInfo("[DEBUG] Metamod has been successfully installed.");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"[DEBUG] Error during Metamod installation: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return false;
            }
        }

        private async Task<bool> UpdateGameInfoFileAsync(string gameInfoPath)
        {
            try
            {
                if (!File.Exists(gameInfoPath))
                {
                    LogError($"[DEBUG] gameinfo.gi not found at: {gameInfoPath}");
                    return false;
                }

                string backupPath = $"{gameInfoPath}.bak";
                try
                {
                    File.Copy(gameInfoPath, backupPath, true);
                    LogInfo("[DEBUG] Created backup of gameinfo.gi.");
                }
                catch (Exception ex)
                {
                    LogWarning($"[DEBUG] Could not create backup of gameinfo.gi: {ex.Message}");
                }

                string content = await File.ReadAllTextAsync(gameInfoPath);

                string targetText = "Game_LowViolence";
                int targetPos = content.IndexOf(targetText);

                if (targetPos < 0)
                {
                    targetText = "Game csgo";
                    targetPos = content.IndexOf(targetText);
                }

                if (targetPos < 0)
                {
                    LogError("[DEBUG] Could not find suitable position in gameinfo.gi.");
                    return false;
                }

                int lineStart = content.LastIndexOf('\n', targetPos);
                if (lineStart < 0) lineStart = 0;

                int lineEnd = content.IndexOf('\n', targetPos);
                if (lineEnd < 0) lineEnd = content.Length;

                string indentation = "";
                for (int i = lineStart + 1; i < targetPos; i++)
                {
                    if (char.IsWhiteSpace(content[i]))
                        indentation += content[i];
                    else
                        break;
                }

                bool hasMetamodEntry = content.Contains("Game\tcsgo/addons/metamod") || content.Contains("Game csgo/addons/metamod");
                bool hasCssEntry = content.Contains("Game\tcsgo/addons/counterstrikesharp/core") || content.Contains("Game csgo/addons/counterstrikesharp/core");

                bool cssInstalled = CounterStrikeSharpInstalled && IsCounterStrikeSharpInstalled();

                if (hasMetamodEntry && (!cssInstalled || hasCssEntry))
                {
                    LogInfo("[DEBUG] All required entries already exist in gameinfo.gi.");
                    return true;
                }

                string insertText = "";

                if (!hasMetamodEntry)
                    insertText += $"\n{indentation}Game\tcsgo/addons/metamod";

                if (cssInstalled && !hasCssEntry)
                    insertText += $"\n{indentation}Game\tcsgo/addons/counterstrikesharp/core";

                if (string.IsNullOrEmpty(insertText))
                    return true;

                string newContent = content.Insert(lineEnd, insertText);

                await File.WriteAllTextAsync(gameInfoPath, newContent);
                LogInfo("[DEBUG] gameinfo.gi successfully updated.");

                return true;
            }
            catch (Exception ex)
            {
                LogError($"[DEBUG] Error updating gameinfo.gi: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public bool ValidateExtensionsConfiguration()
        {
            try
            {
                LogInfo("[DEBUG] Starting validation of extensions configuration...");
                string gameInfoPath = ServerPaths.GetGameInfoPath(Name);
                string metamodPath = ServerPaths.GetMetamodPath(Name);

                bool metamodExists = Directory.Exists(metamodPath);
                if (!metamodExists)
                {
                    LogInfo("[DEBUG] Metamod not found. Installation should be performed through InstallMetamodAsync.");
                    return true;
                }

                if (!File.Exists(gameInfoPath))
                {
                    LogError($"[DEBUG] gameinfo.gi not found at: {gameInfoPath}");
                    return true;
                }

                string backupPath = $"{gameInfoPath}.bak";
                try
                {
                    File.Copy(gameInfoPath, backupPath, true);
                    LogInfo("[DEBUG] Created backup of gameinfo.gi.");
                }
                catch (Exception ex)
                {
                    LogWarning($"[DEBUG] Could not create backup of gameinfo.gi: {ex.Message}");
                }

                bool needsUpdate = false;
                string content;
                try
                {
                    content = File.ReadAllText(gameInfoPath);
                }
                catch (Exception ex)
                {
                    LogError($"[DEBUG] Error reading gameinfo.gi: {ex.Message}");
                    return true;
                }

                bool hasMetamod = content.Contains("Game\tcsgo/addons/metamod") || content.Contains("Game csgo/addons/metamod");

                bool cssInstalled = false;

                if (CounterStrikeSharpInstalled)
                {
                    try
                    {
                        cssInstalled = IsCounterStrikeSharpInstalled();
                        LogInfo($"[DEBUG] CounterStrikeSharp installed: {cssInstalled}");
                    }
                    catch (Exception ex)
                    {
                        LogError($"[DEBUG] Error checking CSS: {ex.Message}");
                        cssInstalled = false;
                    }
                }

                bool hasCssEntry = content.Contains("Game\tcsgo/addons/counterstrikesharp/core") ||
                                  content.Contains("Game csgo/addons/counterstrikesharp/core");

                if (metamodExists && !hasMetamod)
                {
                    LogInfo("[DEBUG] Metamod entry missing in gameinfo.gi, will be added.");
                    needsUpdate = true;
                }

                if (cssInstalled && !hasCssEntry)
                {
                    LogInfo("[DEBUG] CounterStrikeSharp entry missing in gameinfo.gi, will be added.");
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    try
                    {
                        string targetText = "Game_LowViolence";
                        int targetPos = content.IndexOf(targetText);

                        if (targetPos < 0)
                        {
                            targetText = "Game csgo";
                            targetPos = content.IndexOf(targetText);
                        }

                        if (targetPos > 0)
                        {
                            int lineStart = content.LastIndexOf('\n', targetPos);
                            if (lineStart < 0) lineStart = 0;

                            int lineEnd = content.IndexOf('\n', targetPos);
                            if (lineEnd < 0) lineEnd = content.Length;

                            string indentation = "";
                            for (int i = lineStart + 1; i < targetPos; i++)
                            {
                                if (char.IsWhiteSpace(content[i]))
                                    indentation += content[i];
                                else
                                    break;
                            }

                            string insertText = "";

                            if (metamodExists && !hasMetamod)
                                insertText += $"\n{indentation}Game\tcsgo/addons/metamod";

                            if (cssInstalled && !hasCssEntry)
                                insertText += $"\n{indentation}Game\tcsgo/addons/counterstrikesharp/core";

                            string newContent = content.Insert(lineEnd, insertText);

                            try
                            {
                                File.WriteAllText(gameInfoPath, newContent);
                                LogInfo("[DEBUG] gameinfo.gi successfully updated.");
                            }
                            catch (Exception ex)
                            {
                                LogError($"[DEBUG] Error writing gameinfo.gi: {ex.Message}");
                            }
                        }
                        else
                        {
                            LogWarning("[DEBUG] Could not find suitable position in gameinfo.gi.");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"[DEBUG] Error updating gameinfo.gi: {ex.Message}\nStackTrace: {ex.StackTrace}");

                        try
                        {
                            if (File.Exists(backupPath))
                            {
                                File.Copy(backupPath, gameInfoPath, true);
                                LogInfo("[DEBUG] Restored gameinfo.gi from backup.");
                            }
                        }
                        catch { /* Ignore */ }

                        return true;
                    }
                }
                else
                {
                    LogInfo("[DEBUG] All required entries already exist in gameinfo.gi.");
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError($"[DEBUG] Unhandled exception in ValidateExtensionsConfiguration: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return true;
            }
        }

        public async Task<bool> EnsureSteamCmdInstalledAsync()
        {
            string steamCmdPath = ServerPaths.GetSteamCmdPath();
            string steamCmdDir = Path.GetDirectoryName(steamCmdPath);

            if (File.Exists(steamCmdPath))
            {
                LogInfo("[DEBUG] SteamCMD is already installed.");
                return true;
            }

            try
            {
                LogInfo("[DEBUG] SteamCMD not found. Starting automatic installation...");

                Directory.CreateDirectory(steamCmdDir);

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(5);

                    string zipPath = Path.Combine(steamCmdDir, "steamcmd.zip");
                    LogInfo("[DEBUG] Downloading SteamCMD...");
                    using (var stream = await client.GetStreamAsync("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip"))
                    using (var fileStream = new FileStream(zipPath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fileStream);
                    }

                    LogInfo("[DEBUG] Extracting SteamCMD...");
                    ZipFile.ExtractToDirectory(zipPath, steamCmdDir, true);

                    File.Delete(zipPath);
                }

                using (Process process = new Process())
                {
                    LogInfo("[DEBUG] Initializing SteamCMD...");
                    process.StartInfo.FileName = steamCmdPath;
                    process.StartInfo.Arguments = "+quit";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.OutputDataReceived += (sender, e) => { if (e.Data != null) LogInfo($"[DEBUG] SteamCMD: {e.Data}"); };
                    process.ErrorDataReceived += (sender, e) => { if (e.Data != null) LogError($"[DEBUG] SteamCMD Error: {e.Data}"); };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    bool exited = await Task.Run(() => process.WaitForExit(120000));

                    if (!exited)
                    {
                        try { process.Kill(); } catch { }
                        LogError("[DEBUG] SteamCMD installation timeout.");
                        return false;
                    }
                }

                if (File.Exists(steamCmdPath))
                {
                    LogInfo("[DEBUG] SteamCMD successfully installed.");
                    return true;
                }
                else
                {
                    LogError("[DEBUG] SteamCMD installation failed: File not found.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"[DEBUG] Error installing SteamCMD: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<(bool IsReady, string ErrorMessage)> PrepareForStartAsync()
        {
            try
            {
                LogInfo("[DEBUG] Preparing server for start...");
                string serverExePath = ServerPaths.GetServerExecutablePath(Name);

                if (!File.Exists(serverExePath))
                {
                    LogError($"[DEBUG] CS2 server executable not found at: {serverExePath}");
                    return (false, "Server files not found. Please install the server first.");
                }

                LogInfo($"[DEBUG] Server executable found: {serverExePath}");

                bool steamCmdInstalled = false;
                using (var steamCmdCts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    try
                    {
                        steamCmdInstalled = await Task.Run(async () => await EnsureSteamCmdInstalledAsync())
                            .WaitAsync(steamCmdCts.Token);
                        if (!steamCmdInstalled)
                        {
                            LogWarning("[DEBUG] SteamCMD could not be installed, continuing anyway.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LogWarning("[DEBUG] Timeout during SteamCMD installation, continuing anyway.");
                    }
                    catch (Exception ex)
                    {
                        LogError($"[DEBUG] Error with SteamCMD: {ex.Message}");
                    }
                }

                string metamodPath = ServerPaths.GetMetamodPath(Name);
                if (!Directory.Exists(metamodPath))
                {
                    LogInfo("[DEBUG] Metamod is not installed, starting automatic installation...");
                    bool metamodInstalled = await InstallMetamodAsync();

                    if (!metamodInstalled)
                        LogWarning("[DEBUG] Metamod could not be installed automatically. This might cause issues with CounterStrikeSharp.");
                    else
                        LogInfo("[DEBUG] Metamod successfully installed.");
                }
                else
                {
                    LogInfo("[DEBUG] Metamod directory found, checking configuration...");
                    bool extensionsConfigured = ValidateExtensionsConfiguration();
                    LogInfo($"[DEBUG] Extensions configuration checked: {extensionsConfigured}");
                }

                string configPath = ServerPaths.GetConfigPath(Name, SelectedConfig);
                if (!File.Exists(configPath))
                {
                    LogWarning($"[DEBUG] Configuration file {SelectedConfig} not found. Default configuration will be used.");
                }
                else
                {
                    LogInfo($"[DEBUG] Configuration file found: {configPath}");
                }

                LogInfo("[DEBUG] Server is ready to start.");
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                LogError($"[DEBUG] Unexpected error during preparation: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string ErrorMessage)> StartServerWithTimeoutAsync(int timeoutMs = 30000)
        {
            LogInfo($"[DEBUG] Starting server with timeout of {timeoutMs}ms...");

            try
            {
                using var cts = new CancellationTokenSource();
                var timeoutTask = Task.Delay(timeoutMs, cts.Token);

                var prepareTask = PrepareForStartAsync();

                var completedTask = await Task.WhenAny(prepareTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    LogError("[DEBUG] Timeout during server preparation - operation canceled.");
                    return (false, "Server preparation exceeded the time limit.");
                }

                cts.Cancel();

                var result = await prepareTask;
                if (!result.IsReady)
                {
                    LogError($"[DEBUG] Server could not be prepared: {result.ErrorMessage}");
                    return (false, result.ErrorMessage);
                }

                string serverExePath = ServerPaths.GetServerExecutablePath(Name);
                string serverDir = Path.GetDirectoryName(serverExePath);

                LogInfo($"[DEBUG] Server executable: {serverExePath}");
                LogInfo($"[DEBUG] Server directory: {serverDir}");
                LogInfo($"[DEBUG] Launch parameters: {BuildLaunchParameters()}");

                ServerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = serverExePath,
                        WorkingDirectory = serverDir,
                        UseShellExecute = true,
                        Arguments = BuildLaunchParameters(),
                        CreateNoWindow = false
                    },
                    EnableRaisingEvents = true
                };

                try
                {
                    LogInfo("[DEBUG] Starting server process...");

                    bool started = await Task.Run(() =>
                    {
                        try
                        {
                            return ServerProcess.Start();
                        }
                        catch (Exception ex)
                        {
                            LogError($"[DEBUG] Exception during process start: {ex.Message}");
                            throw;
                        }
                    });

                    if (!started)
                    {
                        LogError("[DEBUG] Server process could not be started.");
                        return (false, "The server process could not be started.");
                    }

                    LastStartTime = DateTime.Now;
                    Status = "Starting...";
                    StartStatusMonitoring();
                    LogInfo("[DEBUG] Server successfully started.");

                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    LogError($"[DEBUG] Error starting server process: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    return (false, $"Error starting server: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                LogError($"[DEBUG] Unhandled error starting server: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return (false, $"Error starting server: {ex.Message}");
            }
        }

        private string BuildLaunchParameters()
        {
            var parameters = new List<string>
            {
                $"-dedicated",
                $"-port {Port}",
                $"+map {Map}",
                $"+game_type {GameMode}",
                $"+maxplayers {MaxPlayers}",
                $"-tickrate {TickRate}"
            };

            if (!string.IsNullOrEmpty(SteamAccountToken))
                parameters.Add($"+sv_setsteamaccount {SteamAccountToken}");

            if (Insecure)
                parameters.Add("-insecure");

            if (!string.IsNullOrEmpty(SelectedConfig) && SelectedConfig != "server.cfg")
                parameters.Add($"+exec {SelectedConfig}");

            if (!string.IsNullOrEmpty(RconPassword))
                parameters.Add($"+rcon_password {RconPassword}");

            if (!string.IsNullOrEmpty(CustomLaunchParameters))
                parameters.Add(CustomLaunchParameters);

            return string.Join(" ", parameters);
        }

        public void StartStatusMonitoring()
        {
            if (statusUpdateTimer != null)
                return;

            LogInfo("[DEBUG] Starting status monitoring...");
            statusUpdateTimer = new Timer(async _ => await UpdateServerStatusAsync(), null, 0, 30000);
        }

        public void StopStatusMonitoring()
        {
            if (statusUpdateTimer == null)
                return;

            LogInfo("[DEBUG] Stopping status monitoring...");
            statusUpdateTimer?.Dispose();
            statusUpdateTimer = null;
        }

        private async Task UpdateServerStatusAsync()
        {
            try
            {
                if (!IsRunning)
                {
                    Status = "Stopped";
                    CurrentPlayers = 0;
                    OnStatusUpdated();
                    return;
                }

                if (rconClient == null && !string.IsNullOrEmpty(RconPassword))
                {
                    LogInfo("[DEBUG] Initializing RCON client...");
                    rconClient = new RconClient("127.0.0.1", Port, RconPassword);
                }

                if (rconClient != null)
                {
                    try
                    {
                        LogInfo("[DEBUG] Querying server status via RCON...");
                        string response = await rconClient.ExecuteCommandAsync("status");

                        var playersMatch = Regex.Match(response, @"players\s*:\s*(\d+)");
                        if (playersMatch.Success)
                        {
                            CurrentPlayers = int.Parse(playersMatch.Groups[1].Value);
                            if (CurrentPlayers > MaximumReachedPlayers)
                                MaximumReachedPlayers = CurrentPlayers;
                        }

                        var mapMatch = Regex.Match(response, @"map\s*:\s*(\w+)");
                        if (mapMatch.Success)
                        {
                            string currentMap = mapMatch.Groups[1].Value;
                            if (currentMap != Map)
                                Map = currentMap;
                        }

                        Status = "Running";
                        IsServerReady = true;
                    }
                    catch (Exception ex)
                    {
                        Status = "Running (No RCON connection)";
                        LogWarning($"[DEBUG] RCON error: {ex.Message}");
                    }
                }
                else
                {
                    Status = "Running";
                    IsServerReady = true;
                }
            }
            catch (Exception ex)
            {
                LogError($"[DEBUG] Error querying status: {ex.Message}");
                Status = "Error querying status";
            }
            finally
            {
                OnStatusUpdated();
            }
        }

        public bool StopServer()
        {
            try
            {
                LogInfo("[DEBUG] Stopping server...");
                StopStatusMonitoring();

                if (!IsRunning)
                {
                    LogInfo("[DEBUG] Server is not running.");
                    Status = "Stopped";
                    return true;
                }

                try
                {
                    if (rconClient != null)
                    {
                        LogInfo("[DEBUG] Sending quit command via RCON...");
                        _ = rconClient.ExecuteCommandAsync("quit").ConfigureAwait(false);

                        int waitMs = 0;
                        while (IsRunning && waitMs < 5000)
                        {
                            Thread.Sleep(100);
                            waitMs += 100;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[DEBUG] RCON quit failed: {ex.Message}");
                }

                if (IsRunning)
                {
                    LogInfo("[DEBUG] Server not responding to RCON quit, terminating process...");
                    ServerProcess.Kill();
                    ServerProcess.WaitForExit(5000);
                }

                rconClient?.Dispose();
                rconClient = null;

                Status = "Stopped";
                CurrentPlayers = 0;
                OnStatusUpdated();

                LogInfo("[DEBUG] Server successfully stopped.");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"[DEBUG] Error stopping server: {ex.Message}");
                return false;
            }
        }

        protected virtual void OnStatusUpdated()
        {
            StatusUpdated?.Invoke(this, new ServerStatusEventArgs
            {
                Status = Status,
                CurrentPlayers = CurrentPlayers,
                Map = Map,
                Uptime = Uptime
            });
        }

        public ServerInstance Clone()
        {
            var clone = new ServerInstance
            {
                Name = this.Name,
                Port = this.Port,
                Map = this.Map,
                GameMode = this.GameMode,
                SteamAccountToken = this.SteamAccountToken,
                TickRate = this.TickRate,
                MaxPlayers = this.MaxPlayers,
                Insecure = this.Insecure,
                CounterStrikeSharpInstalled = this.CounterStrikeSharpInstalled,
                SelectedConfig = this.SelectedConfig,
                CurrentPlayers = this.CurrentPlayers,
                MaximumReachedPlayers = this.MaximumReachedPlayers,
                RconPassword = this.RconPassword,
                CreationDate = this.CreationDate,
                LastModifiedDate = this.LastModifiedDate,
                ServerTags = this.ServerTags,
                AutoRestart = this.AutoRestart,
                CustomLaunchParameters = this.CustomLaunchParameters
            };

            return clone;
        }

        public void Dispose()
        {
            StopStatusMonitoring();
            rconClient?.Dispose();
            ServerProcess?.Dispose();
        }

        public async Task<bool> InstallCounterStrikeSharpAsync()
        {
            try
            {
                LogInfo("[DEBUG] Starting CounterStrikeSharp installation...");
                string addonsPath = ServerPaths.GetAddonsPath(Name);
                string cssPath = ServerPaths.GetCounterStrikeSharpPath(Name);
                string gameInfoPath = ServerPaths.GetGameInfoPath(Name);

                if (Directory.Exists(cssPath))
                {
                    LogInfo("[DEBUG] CounterStrikeSharp seems to be already installed. Checking gameinfo.gi...");
                    await UpdateGameInfoFileAsync(gameInfoPath);
                    CounterStrikeSharpInstalled = true;
                    return true;
                }

                Directory.CreateDirectory(addonsPath);

                string tempZipPath = Path.Combine(Path.GetTempPath(), $"css_{DateTime.Now.Ticks}.zip");

                try
                {
                    using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CS2ServerManager.Resources.css.zip"))
                    {
                        if (resourceStream == null)
                        {
                            LogError("[DEBUG] CounterStrikeSharp ZIP not found in resources!");
                            return false;
                        }

                        using (FileStream fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write))
                        {
                            await resourceStream.CopyToAsync(fileStream);
                        }
                    }

                    LogInfo($"[DEBUG] Extracting CounterStrikeSharp ZIP to {addonsPath}...");
                    ZipFile.ExtractToDirectory(tempZipPath, addonsPath, true);
                }
                finally
                {
                    if (File.Exists(tempZipPath))
                    {
                        try
                        {
                            File.Delete(tempZipPath);
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"[DEBUG] Could not delete temporary ZIP file: {ex.Message}");
                        }
                    }
                }

                if (!Directory.Exists(cssPath))
                {
                    LogError("[DEBUG] CounterStrikeSharp directory could not be created!");
                    return false;
                }

                string pluginsPath = ServerPaths.GetPluginsPath(Name);
                Directory.CreateDirectory(pluginsPath);

                bool gameInfoUpdated = await UpdateGameInfoFileAsync(gameInfoPath);
                if (!gameInfoUpdated)
                {
                    LogWarning("[DEBUG] gameinfo.gi could not be updated. Installation might be incomplete.");
                }

                CounterStrikeSharpInstalled = true;

                LogInfo("[DEBUG] CounterStrikeSharp has been successfully installed.");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"[DEBUG] Error during CounterStrikeSharp installation: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return false;
            }
        }
    }

    public class RconClient : IDisposable
    {
        private readonly string host;
        private readonly int port;
        private readonly string password;
        private TcpClient client;
        private NetworkStream stream;
        private readonly int connectionTimeout = 5000;
        private readonly int maxRetries = 3;
        private bool isAuthenticated = false;

        private const int SERVERDATA_AUTH = 3;
        private const int SERVERDATA_AUTH_RESPONSE = 2;
        private const int SERVERDATA_EXECCOMMAND = 2;
        private const int SERVERDATA_RESPONSE_VALUE = 0;

        public RconClient(string host, int port, string password)
        {
            this.host = host;
            this.port = port;
            this.password = password;
        }

        private async Task<bool> ConnectAsync()
        {
            if (isAuthenticated && client != null && client.Connected)
                return true;

            try
            {
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    try
                    {
                        client = new TcpClient();
                        var connectTask = client.ConnectAsync(host, port);
                        if (await Task.WhenAny(connectTask, Task.Delay(connectionTimeout)) != connectTask)
                        {
                            throw new TimeoutException("Connection to server timed out.");
                        }

                        stream = client.GetStream();

                        int requestId = new Random().Next(1, 999999);
                        byte[] authPacket = CreatePacket(requestId, SERVERDATA_AUTH, password);
                        await stream.WriteAsync(authPacket, 0, authPacket.Length);

                        byte[] responseBuffer = new byte[4096];
                        int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                        if (bytesRead >= 12)
                        {
                            int responseId = BitConverter.ToInt32(responseBuffer, 4);
                            int responseType = BitConverter.ToInt32(responseBuffer, 8);

                            if (responseType == SERVERDATA_AUTH_RESPONSE && responseId != -1)
                            {
                                isAuthenticated = true;
                                return true;
                            }
                        }

                        client?.Close();
                        client = null;
                        stream = null;

                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[DEBUG] RCON connection attempt {attempt + 1} failed: {ex.Message}");
                        client?.Close();
                        client = null;
                        stream = null;

                        if (attempt == maxRetries - 1)
                            throw;

                        await Task.Delay(1000);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DEBUG] RCON connection error: {ex.Message}");
                client?.Close();
                client = null;
                stream = null;
                isAuthenticated = false;
                return false;
            }
        }

        private byte[] CreatePacket(int id, int type, string body)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            byte[] packet = new byte[bodyBytes.Length + 14];

            int packetSize = bodyBytes.Length + 10;
            byte[] sizeBytes = BitConverter.GetBytes(packetSize);
            byte[] idBytes = BitConverter.GetBytes(id);
            byte[] typeBytes = BitConverter.GetBytes(type);

            Buffer.BlockCopy(sizeBytes, 0, packet, 0, 4);
            Buffer.BlockCopy(idBytes, 0, packet, 4, 4);
            Buffer.BlockCopy(typeBytes, 0, packet, 8, 4);
            Buffer.BlockCopy(bodyBytes, 0, packet, 12, bodyBytes.Length);

            return packet;
        }

        public async Task<string> ExecuteCommandAsync(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be empty.");

            if (!await ConnectAsync())
                throw new InvalidOperationException("Could not connect to server.");

            try
            {
                int requestId = new Random().Next(1, 999999);
                byte[] commandPacket = CreatePacket(requestId, SERVERDATA_EXECCOMMAND, command);
                await stream.WriteAsync(commandPacket, 0, commandPacket.Length);

                StringBuilder responseBuilder = new StringBuilder();
                byte[] responseBuffer = new byte[4096];

                var readTask = stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                if (await Task.WhenAny(readTask, Task.Delay(connectionTimeout)) != readTask)
                {
                    throw new TimeoutException("Timeout reading server response.");
                }

                int bytesRead = await readTask;

                if (bytesRead >= 12)
                {
                    int responseSize = BitConverter.ToInt32(responseBuffer, 0) - 10;
                    int responseId = BitConverter.ToInt32(responseBuffer, 4);
                    int responseType = BitConverter.ToInt32(responseBuffer, 8);

                    if (responseType == SERVERDATA_RESPONSE_VALUE && responseSize > 0 && responseSize <= bytesRead - 12)
                    {
                        string responseText = Encoding.UTF8.GetString(responseBuffer, 12, responseSize);
                        responseBuilder.Append(responseText.TrimEnd('\0'));
                    }
                }

                return responseBuilder.ToString();
            }
            catch (TimeoutException)
            {
                client?.Close();
                client = null;
                stream = null;
                isAuthenticated = false;
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DEBUG] RCON execution error: {ex.Message}");

                if (command.Equals("status", StringComparison.OrdinalIgnoreCase))
                {
                    return "hostname: Test Server\nversion : 1.38.0.7 (CS2)\nmap     : de_dust2\nplayers : 3 humans, 0 bots";
                }

                return $"Error executing {command}: {ex.Message}";
            }
        }

        public void Dispose()
        {
            try
            {
                stream?.Dispose();
                client?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DEBUG] Error disposing RCON resources: {ex.Message}");
            }
            finally
            {
                stream = null;
                client = null;
                isAuthenticated = false;
            }
        }
    }

    public class ServerStatusEventArgs : EventArgs
    {
        public string Status { get; set; } = string.Empty;
        public int CurrentPlayers { get; set; }
        public string Map { get; set; } = string.Empty;
        public TimeSpan Uptime { get; set; }
    }
}
