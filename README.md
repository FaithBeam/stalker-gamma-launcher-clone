# Stalker GAMMA Launcher Clone

My [stalker-gamma-cli](https://github.com/FaithBeam/stalker-gamma-cli) project is preferred over this project. Its faster, easier to work with, and more reliable.

A clone of Grokitach's Stalker GAMMA Launcher with WINE compatibility and extra features.

<img width="1419" height="1369" alt="image" src="https://github.com/user-attachments/assets/e879c8cd-a451-41e5-9c09-5f1d39ca2c0c" />


## Features

- 2-3X faster than the original GAMMA Launcher [Performance](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/Performance)
- [Backups](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/Backups)
- [Gamma updates information](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/Gamma-Updates-Tab)
- [ModDb updates information](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/ModDb-Updates-Tab)
- [Self Update Dialog](https://github.com/FaithBeam/stalker-gamma-launcher-clone/pull/36)

## Usage

### Windows

1. Download the latest version from the [releases](https://github.com/FaithBeam/stalker-gamma-launcher-clone/releases) page
2. Extract the zip in the same directory as the `.Grok's Modpack Installer` folder so `stalker-gamma-gui.exe` is next to `G.A.M.M.A. Launcher.exe`
3. Run `stalker-gamma-gui.exe`
4. First install initialization
5. Enable long paths
6. (Optional but recommended) Add defender exclusions
7. Full Install
8. Play

### Linux

Installation instructions in the wiki: [Linux install](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/Linux-Install)

### MacOS

Installation instructions in the wiki: [MacOS install](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/MacOS-Install)

## Publishing an Exe

### Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

### Command

`dotnet publish stalker-gamma-gui/stalker-gamma-gui.csproj -c Release -r win-x64 -o bin`

stalker-gamma-gui.exe is in the bin folder.

## Development

Development is only supported on Windows for now.

### Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Gamma RC3 extracted to the `stalker-gamma-gui/bin/Debug/net10.0` folder
