<div align="center">

<img src="assets/winside-banner.png" alt="Winside Banner" width="100%" />

### Next-Gen Windows 11 Deployment, Hardware Compatibility & Bypass Suite

[![GitHub](https://img.shields.io/badge/GitHub-emireln%2Fwinside-blue?logo=github)](https://github.com/emireln/winside)
[![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010-0078D4?logo=windows)](https://github.com/emireln/winside)
[![.NET](https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?logo=dotnet)](https://github.com/emireln/winside)
[![Author](https://img.shields.io/badge/Portfolio-emirln.com-0088FF)](https://emirln.com)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

</div>

---

**Winside** is a clean, modern, and high-performance Windows 11 deployment, hardware compatibility audit, and bypass management tool built with **C# (.NET 8 WPF)**. It features a minimalist icon-only sidebar, instant bilingual localization (PT-BR / EN-US), and a sleek high-contrast dark theme.

Developed by **Emir Tech Tools** • Portfolio: [emirln.com](https://emirln.com)

---

## Key Features

- **Minimalist Icon-Only Interface**:
  - Ultra-clean 56px vertical icon navigation rail with native tooltips and distraction-free layout.
  - High-DPI subpixel ClearType rendering and layout pixel snapping for razor-sharp visual clarity.
  - Electric blue and midnight slate dark color palette aligned with the official Winside logo.
- **Dynamic Bilingual Localization (PT-BR / EN-US)**:
  - Instant one-click language toggle between **Português do Brasil** and **English**.
  - 100% localized UI text, tooltips, checkboxes, audit badges, guidance notes, and alerts.
- **Hardware Compatibility Audit**:
  - Accurate OS detection for Windows 11 (including 24H2/25H2 build revisions).
  - Hardware instruction set detection for **SSE4.2** and **POPCNT** (mandatory for Windows 11 24H2+).
  - Real-time audit for **TPM 2.0**, **Secure Boot**, **Total RAM**, **Disk Capacity**, and **UEFI / BIOS** firmware mode.
- **Bypass & Registry Configuration**:
  - One-click toggles for `LabConfig` bypass keys (`BypassTPMCheck`, `BypassSecureBootCheck`, `BypassRAMCheck`, `BypassCPUCheck`, `BypassStorageCheck`).
  - `MoSetup` in-place upgrade bypass (`AllowUpgradesWithUnsupportedTPMOrCPU`).
  - `OOBE\BypassNRO` toggle to enable offline setup without a mandatory Microsoft Account (MSA).
  - One-click active registry verification and factory reset to default Windows settings.
- **Automated ISO Manager & Fast Extractor**:
  - Direct shortcut to official Microsoft Windows 11 download portal.
  - Automatic native virtual disk image mounting (`Mount-DiskImage`) and drive detection.
  - Multi-threaded file extraction with live progress bar and file counters.
  - Safe automatic unmounting (`Dismount-DiskImage`).
- **Setup Execution Modes**:
  - **Server Upgrade Mode (Recommended Bypass)**: Launches setup with `/product server` to safely bypass in-place upgrade hardware gates.
  - **Standard Setup Mode**: Standard Windows 11 setup wizard.
  - **Custom Switch Mode**: User-defined setup flags (e.g. `/auto upgrade /quiet`).
- **Live Diagnostics & Logging**:
  - Real-time color-coded diagnostic console log.
  - Automatic desktop file synchronization to `Desktop\Winside_install_log.txt`.

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
│   ├── winside-banner.svg    # Vector banner
│   ├── winside-logo-squircle.svg # Desktop squircle logo
│   ├── winside-icon.svg      # Theme-colored icon vector
│   ├── winside-logo.png      # High-res app logo
│   └── winside.ico           # Windows application icon
├── AGENTS.md                 # Agent configuration & architecture notes
├── LICENSE                   # MIT License
├── README.md                 # Project documentation
└── Winside/                  # Winside C# .NET 8 WPF Application
    ├── Winside.csproj
    ├── app.manifest          # RequireAdministrator execution level
    ├── winside.ico           # Application icon
    ├── winside-logo.png      # High-resolution embedded brand asset
    ├── App.xaml / App.xaml.cs
    ├── MainWindow.xaml / MainWindow.xaml.cs
    ├── Models/
    │   ├── HardwareAuditResult.cs
    │   └── InstallTweakOptions.cs
    ├── Services/
    │   ├── DwmHelper.cs             # Windows DWM styling & titlebar customization
    │   ├── IsoManagerService.cs     # ISO mounting & multi-threaded extraction
    │   ├── LocalizationService.cs  # Dynamic bilingual language manager (PT/EN)
    │   ├── LoggerService.cs         # Thread-safe logging subsystem
    │   ├── RegistryBypassService.cs # LabConfig, MoSetup & OOBE registry tweaks
    │   ├── SetupLauncherService.cs  # Windows setup process launcher
    │   └── SystemAuditService.cs    # WMI & hardware instruction set audit
    └── Styles/
        └── ModernTheme.xaml         # High-contrast dark theme & minimalist controls
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

# Build Release
dotnet build -c Release

# Run with Administrator privileges (via PowerShell)
Start-Process .\bin\Release\net8.0-windows\Winside.exe -Verb RunAs
```

### Publish Standalone Executable
```powershell
dotnet publish Winside.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```
The compiled standalone executable will be located in:
`Winside\bin\Release\net8.0-windows\win-x64\publish\Winside.exe`

---

## Author

- **Emir Tech Tools** • Portfolio: [emirln.com](https://emirln.com) • GitHub: [@emireln](https://github.com/emireln)
