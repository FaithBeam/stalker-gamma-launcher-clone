# Requirements

You need these applications installed to run stalker-gamma-cli:

1. git
2. libidn2
3. zstd

You should install them with homebrew:

`brew install git libidn2 zstd`

# Usage

1. Remove the quarantine attribute from the binaries: `xattr -dr com.apple.quarantine .`
2. Install Anomaly and GAMMA in the current directory:

    `./stalker-gamma-cli full-install --anomaly anomaly --gamma gamma --cache cache --download-threads 2`
    
    After install your folder will look like this:
    
    ```
    .
    ├── anomaly
    ├── cache
    └── gamma
    ```

3. Set your WINE prefix to run gamma/ModOrganizer.exe
4. Install these dependencies with winetricks into your prefix:
   - `winetricks d3dcompiler_43 d3dcompiler_47 d3dx10 d3dx11_43 d3dx9 vcrun2022` 