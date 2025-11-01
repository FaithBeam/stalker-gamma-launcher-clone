# Stalker GAMMA Launcher Clone

A clone of Grokitach's Stalker GAMMA Launcher with WINE compatibility and extra features.

<img width="1218" height="1181" alt="image" src="https://github.com/user-attachments/assets/52a21d03-28ed-4883-8614-0c7bbf5aa722" />

## Features

- ~33% faster than the original GAMMA Launcher
- [Backups](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/Backups)
- [Gamma updates information](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/Gamma-Updates-Tab)
- [ModDb updates information](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/ModDb-Updates-Tab)

## First Install Time Taken

|              | Gamma Launcher (Original) | Gamma Launcher Clone (This Repo) | Gamma Launcher (Python) |
|--------------|----------------|----------------------|----------------|
| **System 1** | 24mins         | 16mins               | 22mins            |
| **System 2** | N/A            | 24mins               | 24mins            |
| **System 3** | N/A            | N/A                  | N/A            |

- System 1
  - 8c/16t 7800X3D CPU, NVME
  - Windows 11
- System 2
  - 8c/16t AMD Ryzen 7 8745HS CPU, SSD
  - Linux - CachyOS
- System 3
  - Macbook Pro M4 Max (14c CPU, NVME)

## Usage

### Windows

1. Download the latest version from the [releases](https://github.com/FaithBeam/stalker-gamma-launcher-clone/releases) page
2. Extract the zip in the same directory as the `.Grok's Modpack Installer` folder so `stalker-gamma-gui.exe` is next to `G.A.M.M.A. Launcher.exe`
3. Run `stalker-gamma-gui.exe`
4. First install initialization
5. Enable long paths
6. Install / Update GAMMA
7. Play

### Linux

Installation instructions in the wiki: [Linux install](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/Linux-Install)

### MacOS

Installation instructions in the wiki: [MacOS install](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/MacOS-Install)

## Publishing an Exe

### Requirements

- [.NET SDK 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

### Command

`dotnet publish stalker-gamma-gui/stalker-gamma-gui.csproj -c Release -r win-x64 -o bin`

stalker-gamma-gui.exe is in the bin folder.

## Development

Development is only supported on Windows for now.

### Requirements

- [.NET SDK 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Gamma RC3 extracted to the `stalker-gamma-gui/bin/Debug/net9.0` folder
