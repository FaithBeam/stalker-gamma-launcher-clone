# Stalker GAMMA Launcher Clone

A clone of Grokitach's Stalker GAMMA Launcher with WINE compatibility and extra features.

<img width="1604" height="1460" alt="image" src="https://github.com/user-attachments/assets/d02d36e8-b786-4247-bda3-945d1a503aae" />

[Backup information](https://github.com/FaithBeam/stalker-gamma-launcher-clone/wiki/Backups)

## Usage

### Windows

1. Download the latest version from the [releases](https://github.com/FaithBeam/stalker-gamma-launcher-clone/releases) page
2. Extract the zip in the same directory as the `.Grok's Modpack Installer` folder so `stalker-gamma-gui.exe` is next to `G.A.M.M.A. Launcher.exe`
3. Run `stalker-gamma-gui.exe`
4. First install initialization
5. Install / Update GAMMA (x2)
    - A working install takes two installs. 2nd install you can deselect force git download and force zip extraction
6. Play

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
