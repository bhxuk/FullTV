param(
    [string]$SdkRoot = (Join-Path $env:LOCALAPPDATA 'FullTV\AndroidSdk'),
    [string]$AvdName = 'FullTV_AndroidTV',
    [string]$ImagePackage = 'system-images;android-35;android-tv;x86_64'
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
function Step($message) { Write-Host ("[FullTV] " + $message) -ForegroundColor Cyan }
$cmdVersion = '15859902'
$archiveUrl = "https://dl.google.com/android/repository/commandlinetools-win-${cmdVersion}_latest.zip"
$expectedHash = '90ae805d20434428bffcb699c290860f19bb5f66a67e6b330067e3de801fb04a'
$manager = Join-Path $SdkRoot 'cmdline-tools\latest\bin\sdkmanager.bat'
$avdmanager = Join-Path $SdkRoot 'cmdline-tools\latest\bin\avdmanager.bat'
$emulator = Join-Path $SdkRoot 'emulator\emulator.exe'
$avdHome = Join-Path $env:LOCALAPPDATA 'FullTV\Avds'
New-Item -ItemType Directory -Path $SdkRoot,$avdHome -Force | Out-Null
if (-not (Get-Command java -ErrorAction SilentlyContinue)) {
    throw 'Java 17+ is required for the Android command-line tools. Install a trusted JDK and restart FullTV before continuing.'
}
if (-not (Test-Path $manager)) {
    Step 'Downloading official Android SDK command-line tools (~156 MB)...'
    $zip = Join-Path $env:TEMP 'fulltv-android-commandline-tools.zip'
    Invoke-WebRequest -Uri $archiveUrl -OutFile $zip
    $actualHash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash) { Remove-Item $zip -Force; throw 'SDK checksum mismatch. Download blocked.' }
    $extract = Join-Path $SdkRoot 'cmdline-tools-extract'
    Expand-Archive -Path $zip -DestinationPath $extract -Force
    New-Item -ItemType Directory -Path (Join-Path $SdkRoot 'cmdline-tools') -Force | Out-Null
    Move-Item (Join-Path $extract 'cmdline-tools') (Join-Path $SdkRoot 'cmdline-tools\latest')
    Remove-Item $extract -Recurse -Force
}
$env:ANDROID_HOME = $SdkRoot
$env:ANDROID_SDK_ROOT = $SdkRoot
$env:ANDROID_AVD_HOME = $avdHome
$env:ANDROID_USER_HOME = (Join-Path $env:LOCALAPPDATA 'FullTV\\AndroidUser')
New-Item -ItemType Directory -Path $env:ANDROID_USER_HOME -Force | Out-Null
Step 'Downloading Android Emulator, platform-tools, and Android TV image. SDK licence prompts may appear.'
& $manager "--sdk_root=$SdkRoot" 'platform-tools' 'emulator' $ImagePackage
if ($LASTEXITCODE -ne 0) { throw 'SDK package installation failed. Check Android SDK licence acceptance and the requested system image availability.' }
Step 'Configuring Android TV virtual device...'
$avdIni = Join-Path $avdHome ($AvdName + '.ini')
if (-not (Test-Path $avdIni)) {
    'no' | & $avdmanager create avd -n $AvdName -k $ImagePackage -d 'tv_1080p' --force
    if ($LASTEXITCODE -ne 0) { throw 'AVD creation failed. Check that an Android TV device profile is available in this SDK.' }
}
if (-not (Test-Path $emulator)) { throw 'Android emulator executable missing after SDK installation.' }
Step 'Launching Android TV...'
Start-Process -FilePath $emulator -ArgumentList @('-avd',$AvdName,'-gpu','auto','-no-snapshot-save') -WorkingDirectory (Split-Path $emulator)
Step 'Android TV launch requested. Leave this window open to review any errors.'
