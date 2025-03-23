
# CS2 Server Manager

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

**CS2 Server Manager** is a WPF desktop application built with .NET 8 and C#.
It simplifies the process of creating, configuring, launching, and managing Counter-Strike 2 server instances.  
The application integrates SteamCMD for downloading server files and supports installing and managing plugins such as CounterStrikeSharp and Metamod.

![Dashboard](https://numinux.de/pics/dashboard.png)

---

## Features

- **Server Instance Management**  
  Create, edit, start, stop, and delete server instances. New servers are automatically assigned names and ports, with configs saved in JSON.

- **Automated File Downloads**  
  Server files are fetched via SteamCMD and updated

- **Process Control**  
  Launch or terminate server processes with dynamic configuration loading.

- **Configuration Editing**  
  Modify port, map, game mode, tick rate, max players, and security flags via a settings dialog.

- **Plugin Management**  
  Automatically install plugins like CounterStrikeSharp or upload manually for easy maintenance.

- **Workshop Map Integration**  
  Download and install workshop maps directly within the Server Manager.

---

## Installation

### Prerequisites

- .NET 8 Runtime or SDK  
- Windows OS with folder creation permission in `%USERPROFILE%\Documents`  
- Stable internet connection  

### Steps for End Users

1. **Download the Application**  
   Get the latest release from the [Releases](#) page.

2. **Run the Application**  
   - First launch creates `%USERPROFILE%\Documents\CS2ServerManager`  
   - Downloads and extracts SteamCMD if needed  
   - Loads existing server configs from JSON  

### Steps for Developers

1. **Clone the Repository**
   ```bash
   git clone https://github.com/sledgehamm3r/CS2-ServerManager.git
   cd CS2-ServerManager
   ```

2. **Restore NuGet Packages**  
   Open the solution in Visual Studio and ensure all dependencies are installed.

3. **Build the Project**  
   All projects target .NET 8.

4. **Run the Application**  
   Start via Visual Studio or run the compiled EXE.

---

## Usage

### Creating a New Server

1. Click **Create Server**
2. New instance gets name + port assigned
3. A `settings.json` is generated and shown in the dashboard

![New Server](https://numinux.de/pics/createserver.png)

### Setting Up the Server Environment

1. Select your server
2. Click **Download Server Files ⬇️**
3. Confirm the operation
4. SteamCMD handles the download; console shows progress

![Download](https://numinux.de/pics/download.png)

### Managing Server Processes

- Use **Start ▶️** and **Stop ⏹️** to control processes
- Config is read from `settings.json`

![Server Controls](https://numinux.de/pics/startstop.png)

### Editing Server Settings

1. Click **Edit ✏️**
2. Modify port, map, game mode, token, tick rate, etc.
3. Specify Workshop ID for maps
4. Save to update config

![Settings](https://numinux.de/pics/edit.png)

### Plugin Management

#### Installing CounterStrikeSharp

1. Open Edit Dialog  
2. Enable "Install CounterStrikeSharp"  
3. Save settings – plugin will auto-download & deploy

#### Managing Plugins

1. Check installed plugins in settings  
2. They load automatically with the server

### Downloading Workshop Maps

1. Select server  
2. Enter Workshop ID in settings  
3. Click **Download Workshop Item**  
4. Map becomes available to the server

### Server Configuration Files

- Defaults: `server.cfg`, `5v5.cfg`, `2v2.cfg`, `deathmatch.cfg`, `prac.cfg`  
- Configs located in:  
  `Documents\CS2ServerManager\[ServerName]\CS2ServerFiles\game\csgo\cfg\`

### Console and Logging

- Bottom area = console + status info  
- Use **Toggle Console** to hide/show  
- Logs saved in app directory

![Console](https://numinux.de/pics/console.png)

---

## Advanced Features

### RCON Support

With RCON password set, you can:

- Send remote commands  
- Query server status  
- Properly shut down server  

### Automatic Restart

Enable "Auto Restart" to restart the server on crash.

### Server Tags

Add tags to help users discover your server.

### Custom Launch Parameters

Add your own launch params for advanced configurations.

---

## Troubleshooting

### SteamCMD Issues

- **Download Errors**: Check internet, retry  
- **Not Starting**: Delete SteamCMD folder, restart app

### Server Launch Problems

- **Won’t Start**: Ensure port isn't used  
- **Steam Token Error**: Set a valid Game Server Token

### Plugin Issues

- **CounterStrikeSharp Not Working**: Check Metamod + gameinfo.gi  
- **Plugin Conflicts**: Disable plugins one by one

### Checking Logs

- Check app directory log files  
- Review console output for clues

---

## Roadmap / TO-DO

- [ ] 🔌 **Plugin System**: Installation via URL/ZIP, Config Editor, Enable/Disable, Update Checks  
- [ ] 📊 **Server Monitoring**: Real-time Stats, CPU/Memory Usage, Log Viewer with Filters, Performance Graphs  
- [ ] 🗺️ **Map Management**: Workshop Browser, Custom Rotation, Map Voting  
- [ ] 💾 **Backups**: Scheduled Backups, One-Click Restore, Cloud Integration (Google Drive, Dropbox)  
- [ ] 🧩 **Server Templates**: Predefined Configs, Export/Import, Community Sharing  
- [ ] 👮 **Admin Tools**: Steam ID Management, Permission Groups, Admin Command Interface  
- [ ] 🌐 **Multi-Server Support**: Central Dashboard, Resource Optimization, Cross-Server Player Management  
- [ ] ⚙️ **Config Editor**: GUI-Based Editing, Validation & Error Detection, Version Control  
- [ ] 🔗 **Integrations**: Discord Bot, Web Panel for Remote Control, Mobile Companion App  


---

## Contributing

Contributions are welcome!

1. Fork the repo  
2. Create a branch:
   ```bash
   git checkout -b feature/amazing-feature
   ```
3. Commit your changes:
   ```bash
   git commit -m 'Add some amazing feature'
   ```
4. Push:
   ```bash
   git push origin feature/amazing-feature
   ```
5. Open a Pull Request

Please follow coding standards and include tests if possible.

---

## License

This project is licensed under the **MIT License**.  
See the `LICENSE` file for more info.

---

## Acknowledgments

- 🙏 Valve for CS2 & SteamCMD  
- 🙏 CounterStrikeSharp [Link](https://github.com/roflmuffin/CounterStrikeSharp) & Metamod devs [Link](https://www.metamodsource.net/)   

> *CS2 Server Manager is not affiliated with or endorsed by Valve. Counter-Strike is a registered trademark of Valve Corporation.*
