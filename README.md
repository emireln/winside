<div align="center">

<img src="assets/winside-banner.png" alt="Winside Banner" width="100%" />

### Next-Gen Windows 11 Deployment, Hardware Compatibility & Bypass Suite

[![GitHub](https://img.shields.io/badge/GitHub-emireln%2Fwinside-blue?logo=github)](https://github.com/emireln/winside)
[![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010-0078D4?logo=windows)](https://github.com/emireln/winside)
[![.NET](https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?logo=dotnet)](https://github.com/emireln/winside)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

</div>

---

**Winside** is a modern, high-performance Windows 11 deployment, hardware compatibility audit, and bypass tool written in **C# (.NET 8 WPF)** featuring native **Windows 11 Fluent Mica & Acrylic** design.

Developed by **Emir Tech Tools**, old ETT project (depracated & cancelled).

---

## Key Features

- **Hardware Compatibility Audit**:
  - Heuristic & WMI instruction set detection for **SSE4.2** and **POPCNT** (crucial for Windows 11 24H2/25H2).
  - Live detection for **TPM 2.0**, **Secure Boot**, **Total RAM**, **Disk Space**, and **UEFI / BIOS** firmware mode.
- **Bypass & Registry Configuration**:
  - One-click toggles for `LabConfig` bypass keys (`BypassTPMCheck`, `BypassSecureBootCheck`, `BypassRAMCheck`, `BypassCPUCheck`, `BypassStorageCheck`, `BypassDiskCheck`).
  - `MoSetup` upgrade bypass (`AllowUpgradesWithUnsupportedTPMOrCPU`).
  - `OOBE\BypassNRO` toggle to enable offline setup without a mandatory Microsoft Account (MSA).
  - One-click restore / reset to standard Windows defaults.
- **Automated ISO Manager & Fast Extractor**:
  - Official Microsoft download launcher.
  - Automatic native disk image mounting (`Mount-DiskImage`) and drive detection.
  - Multi-threaded file extraction with live progress bar and speed/file counters.
  - Clean automatic unmounting (`Dismount-DiskImage`).
- **Setup Execution Modes**:
  - **Server Upgrade Mode (Recommended Bypass)**: Launches setup with `/product server` to bypass in-place upgrade hardware gates.
  - **Standard Setup Mode**: Standard Windows 11 installation workflow.
  - **Custom Switch Mode**: User-defined setup flags (e.g. `/auto upgrade /quiet`).
- **Live Diagnostics & Logging**:
  - Real-time color-coded diagnostic event log.
  - Automatic desktop sync to `Desktop\Winside_install_log.txt`.

---

## Project Structure

```
winside/
├── .agents/
│   └── rules/
│       ├── caveman.md        # AI output compression & token optimization
│       └── ponytail.md       # AI clean code & YAGNI guidelines
├── .gitignore                # Visual Studio & .NET ignore list
├── assets/
│   ├── winside-banner.png    # High-resolution README banner
│   ├── winside-banner.svg    # Vector banner (background & centered logo)
│   ├── winside-logo-squircle.svg # Desktop squircle logo
│   ├── winside-icon.svg      # Theme-colored icon vector
│   ├── winside-logo.png      # High-res app logo
│   └── winside.ico           # Windows application icon
├── AGENTS.md                 # Agent configuration
├── GEMINI.md                 # Gemini assistant guidelines
├── LICENSE                   # MIT License
├── README.md                 # Project documentation
└── Winside/                  # Winside C# .NET 8 WPF Application
    ├── Winside.csproj
    ├── app.manifest          # RequireAdministrator elevation
    ├── winside.ico           # Application icon
    ├── App.xaml / App.xaml.cs
    ├── MainWindow.xaml / MainWindow.xaml.cs
    ├── Models/
    ├── Services/
    └── Styles/
        └── ModernTheme.xaml  # Windows 11 Fluent dark palette & controls
```

---

## Building & Running

### Prerequisites
- Windows 10 or Windows 11 (x64)
- .NET 8.0 SDK or newer

### Build & Run
```powershell
# Navigate to application folder
cd Winside

# Build
dotnet build -c Release

# Run
dotnet run -c Release
```

### Publish Standalone Executable
```powershell
dotnet publish Winside.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```
The compiled standalone executable will be located in:
`Winside\bin\Release\net8.0-windows\win-x64\publish\Winside.exe`
