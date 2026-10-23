# FullTV

🌐 **Official website:** **[FullTV — Visit the landing page](https://bhxuk.github.io/FullTV/)**

[![Website](https://img.shields.io/badge/Website-Open%20FullTV-8c8bff?style=for-the-badge)](https://bhxuk.github.io/FullTV/)
[![Download latest Windows EXE](https://img.shields.io/badge/Download-Latest%20Windows%20EXE-40c8a0?style=for-the-badge)](https://github.com/bhxuk/FullTV/releases/latest/download/FullTV-Setup.exe)

**⬇ [Download the latest FullTV Windows installer (.exe)](https://github.com/bhxuk/FullTV/releases/latest/download/FullTV-Setup.exe)**


**FullTV** is a Windows TV-style desktop and Android device management project.

> **Current state: v0.1.0 development preview.** The app does **not** yet bundle or boot Android. It can manage compatible Android apps over ADB after you install Android platform-tools and connect a device or emulator. APK support, Plex, 4K, controller focus navigation and a self-contained Android runtime require further development and validation.

## What works in the current source
- Modern dark, responsive Windows desktop home and clickable app tiles
- APK chooser, ADB install/reinstall, list, launch and uninstall
- ADB TCP connection and local device detection
- Plex web quick-launch and a basic accent setting
- GitHub latest-release update check with SHA-256 verification before starting a new installer
- GitHub Actions workflow for producing Windows installers
- GitHub Pages landing page and changelog

## Developer build
Install .NET 8 SDK on Windows:

```powershell
dotnet restore src/FullTV/FullTV.csproj
dotnet run --project src/FullTV/FullTV.csproj
```

For APK management, install Android SDK platform-tools and add `adb` to PATH, or set its path under Preferences. Enable debugging on an Android device or run a compatible emulator, authorise ADB, then choose **Connect ADB**.

## System requirements
Windows 10/11 x64, .NET 8 runtime for development (public builds self-contained). An accessible Android device/emulator and ADB are required for app management. Hardware virtualisation and Android guest packaging remain planned features.

## Releases and updating
Tagged releases such as `v0.1.0` trigger the Windows packaging workflow. Installer and its SHA-256 checksum are attached to the GitHub Release if the workflow succeeds. The in-app **Check updates** button checks the latest published Release; for a newer version, it downloads and validates the checksum before launching the installer with user confirmation. Note: checksum authenticity relies on the GitHub release channel; production code signing and stronger signature trust are future requirements.

## Roadmap
1. License-audited, bundled Android guest and reliable guest rendering
2. GPU/video and audio acceleration, x86/ARM APK compatibility testing
3. First-run wizard, fullscreen UI, native controller navigation
4. Plex local integration, 1080p/4K real-media benchmarking
5. Robust installer prerequisites, code signing, update rollback, security hardening

## Security and legal
Only install APKs you have permission to use and trust. No proprietary streaming APKs, Google Mobile Services or commercial Android images are included. Do not expose ADB directly to the internet. GPL/third-party license review is mandatory before bundling any Android operating system image, QEMU or platform tools.

See [CHANGELOG.md](CHANGELOG.md), [website](website/index.html) and [LICENSE](LICENSE).
