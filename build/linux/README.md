# Requirements

You need these applications installed to run stalker-gamma-cli:

1. git
2. unzip

Refer to your distro how to install them if they're not installed.

# Usage

1. Install Anomaly and GAMMA in the current directory:

    `./stalker-gamma-cli full-install --anomaly anomaly --gamma gamma --cache cache --download-threads 2`
    
    After install your folder will look like this:
    
    ```
    .
    ├── anomaly
    ├── cache
    └── gamma
    ```

2. Set your WINE prefix to run gamma/ModOrganizer.exe
3. Install these dependencies with winetricks into your prefix:
   - `winetricks d3dcompiler_43 d3dcompiler_47 d3dx10 d3dx11_43 d3dx9 vcrun2022` 