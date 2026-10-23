# FullTV Android TV runtime candidates (23 October 2026)

## Recommended candidate: LineageOS TV x86
- Source organization: https://github.com/LineageOS-TV-x86
- Generic x86_64 TV builds: https://github.com/LineageOS-TV-x86/x86_64_tv/releases
- ISO distribution: https://sourceforge.net/projects/lineageos-tv-x86/files/
- Unlike generic Android-x86 images, this project explicitly targets the Android TV interface.

## Secondary candidate: BlissOS
- https://github.com/BlissOS
- BlissOS is Android for PCs, rather than an Android TV-oriented image.
- New official public ISO publication was paused as of August 2026.

## Integration status
- QEMU/WHPX host control and user-importable ISO/disk boot exist in the Windows source.
- There is no tested integrated guest, preinstalled application catalogue, in-app guest video surface, or universal APK compatibility.
- Booting a live ISO does **not** supply persistent installed apps. A guest installation onto a writable QCOW2 disk is required.
- The current importer does not create that virtual disk or automate Android guest installation.
- QEMU BIOS/UEFI, disk controller, network adapter, ADB and display settings need actual end-to-end testing against a pinned ISO version.
- 4K, DRM, hardware decoding, ARM APK translation and commercial streaming apps are not validated.
- A publicly distributed Android runtime requires source-specific licence audit, attribution and checks for proprietary bundled components (including optional Google services).
- Do not redistribute images that cannot legally be repackaged. User-supplied ISO images are not inherently redistributable.

## Completion criteria before announcing FullTV Android TV edition
1. Pin a legally redistributable Android TV x86_64 image and verify its digest.
2. Boot the image on a clean Windows 10/11 VM with WHPX; test first-run flow.
3. Create writable guest storage, persist Android setup and third-party APKs after restart.
4. Include all necessary QEMU / firmware / ADB binaries subject to their licences.
5. Launch the Android home screen without asking for manual image or executable selection.
6. Implement integrated guest input, audio/video, controller focus and app import.
7. Test APK installation and playback on supported host GPU/CPU configurations.
8. Publish a reproducible GitHub Actions installer with tested full runtime and release signature.

The development preview should not be marketed as a fully integrated Android TV system until these tests pass.
