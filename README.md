# CS2 Server Manager

CS2 Server Manager is a WPF desktop application built with .NET 8. It allows you to easily create, configure, launch, and manage CounterStrike 2 server instances. In addition, the application provides integrated support to download server files via SteamCMD and to manage plugins (such as CounterStrikeSharp and Metamod).

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technologies](#technologies)
- [Installation](#installation)
- [Usage](#usage)
- [Roadmap](#roadmap)
- [License](#license)

---

## Overview

CS2 Server Manager provides a user-friendly interface to manage your CS2 servers. With this application, you can:
- Create new server instances with automatically assigned ports.
- Download and update server files using SteamCMD.
- Start and stop server processes with a simple click.
- Edit server settings such as port, map, game mode, tick rate, and more.  
- Install and manage plugins either automatically (e.g., CounterStrikeSharp) or manually via file uploads.
- Monitor long-running processes through a progress bar and console log in the application's footer.

---

## Features

- **Server Management**: Create, edit, start, stop, and delete counterstrike server instances.
- **Automated Downloads**: SteamCMD integration performs automatic download and update of CS2 server files.
- **Plugin Management**: Download/install/update plugins like CounterStrikeSharp and Metamod, or upload custom plugin ZIP files.
- **Asynchronous Operation**: Tasks such as file downloading and extraction are performed asynchronously to keep the UI responsive.

---

## Technologies

- **.NET 8 & C# 12**: For modern asynchronous programming and efficient runtime performance.
- **WPF (Windows Presentation Foundation)**: For building the rich desktop user interface.
- **HttpClient & Async/Await**: For asynchronous downloading and file operations.
- **JSON Serialization (Newtonsoft.Json & System.Text.Json)**: For saving and loading server settings.
- **NLog**: For robust logging and error reporting.
- **System.IO.Compression**: To extract ZIP files (used for SteamCMD and plugin packages).

---

## Installation

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
- A Windows operating system with permissions to create folders in the Documents directory.

### Steps

1. **Clone or Download the Repository**  
   Clone the repository into Visual Studio 2022.

2. **Restore NuGet Packages**  
   Ensure all dependencies (for example, `Newtonsoft.Json` and `NLog`) are restored.

3. **Build the Project**  
   Compile the solution. All projects are targeting .NET 8.

4. **Run the Application**  
   On first launch:
   - A base folder is created at `%USERPROFILE%\Documents\CS2ServerManager`.
   - SteamCMD is automatically downloaded and extracted if not already present.
   - Any existing server instances are loaded from the JSON configuration files.

---

## Usage

### Creating a New Server

- Click the **Create Server** button.
- A new server instance is created with an automatically assigned name and port.
- A `settings.json` file is generated in the relevant server folder.
- The new instance appears in the dashboard DataGrid.

### Managing Server Processes

- Use the **Start** and **Stop** buttons (▶️ and ⏹️ respectively) in the DataGrid to launch or terminate server processes.
- Server settings are read from the associated `settings.json` file at launch.

### Editing Server Settings

- Click the **Edit** (✏️) button to open the settings dialog.
- Update parameters such as port, map, game mode, Steam account token, tick rate, and max players.
- If a custom map is needed, selecting "custom" in the map selection will reveal an input field for the map name.
- Save the changes to update the configuration file.

### Downloading Server Files

- Click the **Download Server Files** (⬇️) button for the corresponding server instance.
- The process is asynchronous and uses SteamCMD.  
- Progress information and logs are displayed in the footer console panel.

### Plugin Management

- **Automatic Plugin Installation**:  
  Within the edit dialog, you can install plugins like CounterStrikeSharp. The application downloads, extracts, and updates plugin directories accordingly.
  
- **Manual Plugin Upload**:  
  Alternatively, you can upload a plugin ZIP file. The system checks for any existing plugins of the same name and prompts for overwrite confirmation.

  
- **Base Folder Creation**:  
  A directory is created at `%USERPROFILE%\Documents\CS2ServerManager` on the first run. This folder stores server instance data, settings files, SteamCMD, and logs.

## Roadmap

- **Extended Plugin Support**:  
  Integrate more plugins with automated version checks and updates.
- **Enhanced Error Handling**:  
  Improve logging and error messages for troubleshooting.
- **Server Monitoring Dashboard**:  
  Develop a more interactive dashboard to monitor server performance and logs in real time.
- **MVVM Refactoring**:  
  Transition to the MVVM architectural pattern to further separate UI and business logic for maintainability and testability.

## License

This project is licensed under the MIT License – see the [LICENSE](LICENSE) file for details.
