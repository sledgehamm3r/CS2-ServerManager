<div align="center">
  <h1>CS2 Server Manager</h1>
  <p>
    <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8-blue.svg">
    <img alt="License: MIT" src="https://img.shields.io/badge/License-MIT-yellow.svg">
    <img alt="WPF" src="https://img.shields.io/badge/WPF-Desktop-green.svg">
  </p>
</div>

---

## Overview

CS2 Server Manager is a WPF desktop application built with .NET 8 and C# 12. It simplifies the process of creating, configuring, launching, and managing CounterStrike 2 server instances. The application integrates SteamCMD for downloading server files and supports installing and managing plugins such as CounterStrikeSharp and Metamod.

---

## Features

- **Server Instance Management:**  
  Create, edit, start, stop, and delete server instances. New servers are automatically assigned names and ports, and their configurations are saved in JSON files.

- **Automated File Downloads:**  
  Fetched via SteamCMD, server files are downloaded and updated asynchronously without blocking the UI.

- **Process Control:**  
  Easily launch or terminate server processes, with configurations loaded dynamically from stored settings.

- **Configuration Editing:**  
  Edit server properties such as port, map, game mode, tick rate, maximum players, and security flags through a dedicated settings dialog.

- **Plugin Management:**  
  Install plugins automatically (e.g., CounterStrikeSharp) with downloaded ZIP packages or upload them manually, ensuring smooth operation and maintenance.

---

## Technologies

- **.NET 8 & C# 12:** Modern and efficient asynchronous programming.
- **WPF (Windows Presentation Foundation):** Provides a rich desktop user interface.
- **HttpClient & Async/Await:** Ensure asynchronous file operations and downloads.
- **JSON Serialization:** Uses Newtonsoft.Json and System.Text.Json for configuration storage.
- **NLog:** For comprehensive logging and error diagnostics.
- **System.IO.Compression:** Extracts ZIP packages for SteamCMD and plugins.

---

## Installation

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
- A Windows OS with permission to create directories in the Documents folder

### Steps

1. **Clone the Repository**  
   Clone the repository to your local machine and open it in Visual Studio 2022.

2. **Restore NuGet Packages**  
   Ensure all dependencies (e.g., `Newtonsoft.Json`, `NLog`) are properly restored.

3. **Build the Project**  
   Compile the solution—all projects target .NET 8.

4. **Run the Application**  
   On first launch:
   - A base folder is created at `%USERPROFILE%\Documents\CS2ServerManager` to store server data, settings, SteamCMD, and logs.
   - SteamCMD is automatically downloaded and extracted if not available.
   - Existing server configurations (if any) are loaded from JSON files.

---

## Usage

### Creating a New Server
- Click the **Create Server** button.
- A new server instance is generated with an automatically assigned name and port.
- A `settings.json` file is created for that server, and the instance is displayed in the dashboard.

### Managing Server Processes
- Use the **Start** (▶️) and **Stop** (⏹️) buttons in the dashboard to launch or terminate server processes.
- The server reads its configuration from the associated `settings.json` file when starting.

### Editing Server Settings
- Click the **Edit** (✏️) button to open the server settings dialog.
- Update parameters such as port, map, game mode, token, tick rate, and max players.
- For custom maps, selecting “custom” reveals an input field to specify the map.
- Save your changes to update the configuration file.

### Downloading Server Files
- Click the **Download Server Files** (⬇️) button for the desired server instance.
- SteamCMD will download and update the server files asynchronously.
- Progress and log messages appear in the console output panel at the bottom.

### Plugin Management
- **Automatic Plugin Installation:**  
  Within the edit dialog, you can install plugins like CounterStrikeSharp. The application downloads, extracts, and deploys the plugin automatically.
  
- **Manual Plugin Upload:**  
  Alternatively, you can manually upload a plugin ZIP file. The system checks for any existing plugin with the same name and prompts for an overwrite if necessary.

---

## RoadMap / TO-DO

- **Extended Plugin Support:**  
  Integrate more plugins with automated version checking and update mechanisms.

- **Enhanced Error Handling:**  
  Implement advanced logging and user-friendly error messages for smoother troubleshooting.

- **Server Monitoring Dashboard:**  
  Develop a comprehensive dashboard for real-time server performance monitoring and logging.

- **MVVM Refactoring:**  
  Transition the codebase to use the MVVM pattern for improved maintainability and testability.

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---
