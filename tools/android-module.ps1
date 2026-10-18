$ErrorActionPreference = 'Continue'
$dlRoot   = 'D:\Unity\installers'
$sdkZip   = 'D:\Unity\installers\sdk'
$edRoot   = 'D:\Unity\Hub\Editor\6000.3.11f1'
$ap       = "$edRoot\Editor\Data\PlaybackEngines\AndroidPlayer"
$log      = 'D:\Unity\android-module.log'

function Log($m) { $stamp = (Get-Date).ToString('o'); Add-Content -Path $log -Value "$stamp $m" }
function CurlSave($u, $out) {
    Log "DL  -> $out"
    $t0 = Get-Date
    & curl.exe -L --retry 5 --retry-delay 3 --connect-timeout 30 -sS -o $out $u 2>>$log
    $ms = ((Get-Date) - $t0).TotalMilliseconds
    if (Test-Path $out) {
        $sz = (Get-Item $out).Length
        Log "DL  <- $out : $sz bytes in $([int]$ms) ms"
    } else { Log "DL  FAILED $out" }
}

New-Item -ItemType Directory -Force -Path $dlRoot, $sdkZip | Out-Null
Add-Content -Path $log -Value "=== android module start ==="

$urls = @{
    'as.exe'     = 'https://download.unity3d.com/download_unity/3000ef702840/TargetSupportInstaller/UnitySetup-Android-Support-for-Editor-6000.3.11f1.exe'
    'jdk.zip'    = 'https://download.unity3d.com/download_unity/open-jdk/open-jdk-win-x64/jdk17.0.18-8_15e8817d1f5db6db3571ebe7430ef37f7fa8e60e8ff6f3e18ca1cb4c29f78774.zip'
    'ndk.zip'    = 'https://dl.google.com/android/repository/android-ndk-r27c-windows.zip'
    'cmake.zip'  = 'https://dl.google.com/android/repository/cmake-3.22.1-windows.zip'
    'bt.zip'     = 'https://dl.google.com/android/repository/build-tools_r36_windows.zip'
    'pt.zip'     = 'https://dl.google.com/android/repository/platform-tools_r37.0.1-win.zip'
    'p34.zip'    = 'https://dl.google.com/android/repository/platform-34-ext7_r02.zip'
    'p36.zip'    = 'https://dl.google.com/android/repository/platform-36_r02.zip'
    'p37.zip'    = 'https://dl.google.com/android/repository/platform-37.0_r02.zip'
    'cmt.zip'    = 'https://dl.google.com/android/repository/commandlinetools-win-12266719_latest.zip'
}
$files = @{}
foreach ($k in $urls.Keys) {
    $out = Join-Path ($(if ($k -eq 'as.exe' -or $k -eq 'jdk.zip') { $dlRoot } else { $sdkZip })) $k
    if (-not (Test-Path $out)) { CurlSave $urls[$k] $out }
    $files[$k] = $out
}

Add-Content -Path $log -Value "--- downloads done; installing ---"

Add-Type -AssemblyName System.IO.Compression.FileSystem
function Show-ZipRoot($path) {
    $z = [System.IO.Compression.ZipFile]::OpenRead($path)
    $roots = $z.Entries | ForEach-Object { ($_.FullName -split '/')[0] } | Sort-Object -Unique
    $z.Dispose()
    return $roots
}
function Extract-Into($zip, $dest) {
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zip, $dest, $true)
}

# --- 1) Android playback engine (NSIS silent) ---
Add-Content -Path $log -Value "installing Android Support EXE..."
$t0 = Get-Date
Start-Process -FilePath $files['as.exe'] -ArgumentList '/S', "/D=$edRoot" -Wait
Log ("as.exe done in {0}s" -f [int](((Get-Date)-$t0).TotalSeconds))
Log "PlaybackEngines exists: $(Test-Path "$ap")"

# --- 2) OpenJDK ---
try {
    Log "jdk roots: $((Show-ZipRoot $files['jdk.zip']) -join ',')"
    Extract-Into $files['jdk.zip'] $ap
    $child = Get-ChildItem $ap -Directory | Where-Object { $_.Name -ne 'OpenJDK' } | Select-Object -First 1
    if ($child) { Log "jdk child: $($child.Name)" }
} catch { Log "jdk EXTRACT ERROR: $($_.Exception.Message)" }

if (Test-Path "$ap\OpenJDK\bin\java.exe") { Log "OpenJDK OK" }
else {
    # zip probably had its own root folder → move contents
    $jRoot = Get-ChildItem $ap -Directory | Where-Object { $_.Name -match 'jdk|Open' } | Select-Object -First 1
    if ($jRoot) {
        New-Item -ItemType Directory -Force -Path "$ap\OpenJDK" | Out-Null
        Get-ChildItem $jRoot.FullName | Move-Item -Destination "$ap\OpenJDK" -Force
        Remove-Item $jRoot.FullName -Force -Recurse -ErrorAction SilentlyContinue
    }
    Log "OpenJDK after fixup: $(Test-Path "$ap\OpenJDK\bin\java.exe")"
}

# --- 3) NDK ---
try {
    Log "ndk roots: $((Show-ZipRoot $files['ndk.zip']) -join ',')"
    Extract-Into $files['ndk.zip'] "$ap\NDK"
    $ndkChild = Get-ChildItem "$ap\NDK" -Directory | Where-Object { $_.Name -notmatch '^android-ndk' -and $_.Name -notmatch '^r?\d' } | Select-Object -First 5
    $inner = Get-ChildItem "$ap\NDK" -Directory | Where-Object { $_.Name -match '^android-ndk' } | Select-Object -First 1
    if ($inner) {
        Get-ChildItem $inner.FullName | Move-Item -Destination "$ap\NDK" -Force
        Remove-Item $inner.FullName -Force -Recurse -ErrorAction SilentlyContinue
        Log "NDK inner folder flattened"
    }
    Log "NDK source.properties: $(Test-Path "$ap\NDK\source.properties")"
} catch { Log "ndk EXTRACT ERROR: $($_.Exception.Message)" }

# --- 4) SDK pieces ---
# cmake -> SDK\cmake\3.22.1
try {
    Log "cmake roots: $((Show-ZipRoot $files['cmake.zip']) -join ',')"
    Extract-Into $files['cmake.zip'] "$ap\SDK\cmake"
    $c = Get-ChildItem "$ap\SDK\cmake" -Directory | Where-Object { $_.Name -eq 'cmake' } | Select-Object -First 1
    if ($c) { Get-ChildItem $c.FullName | Move-Item -Destination "$ap\SDK\cmake\3.22.1" -Force; Remove-Item $c.FullName -Recurse -Force -EA SilentlyContinue; Log "cmake renamed -> 3.22.1" }
    Log "cmake exe: $(Test-Path "$ap\SDK\cmake\3.22.1\bin\cmake.exe")"
} catch { Log "cmake EXTRACT ERROR: $($_.Exception.Message)" }

# build-tools -> SDK\build-tools\36.0.0
try {
    Log "bt roots: $((Show-ZipRoot $files['bt.zip']) -join ',')"
    Extract-Into $files['bt.zip'] "$ap\SDK\build-tools"
    $b = Get-ChildItem "$ap\SDK\build-tools" -Directory | Where-Object { $_.Name -notmatch '^\d+\.\d+\.\d+$' } | Select-Object -First 1
    if ($b) { Get-ChildItem $b.FullName | Move-Item -Destination "$ap\SDK\build-tools\36.0.0" -Force; Remove-Item $b.FullName -Recurse -Force -EA SilentlyContinue; Log "build-tools renamed -> 36.0.0" }
    Log "aapt2: $(Test-Path "$ap\SDK\build-tools\36.0.0\aapt2.exe")"
} catch { Log "bt EXTRACT ERROR: $($_.Exception.Message)" }

# platform-tools -> SDK\platform-tools
try {
    Log "pt roots: $((Show-ZipRoot $files['pt.zip']) -join ',')"
    Extract-Into $files['pt.zip'] $ap
    Log "adb: $(Test-Path "$ap\platform-tools\adb.exe")"
} catch { Log "pt EXTRACT ERROR: $($_.Exception.Message)" }

# platforms
foreach ($p in @(@('p34.zip','android-34-ext7'), @('p36.zip','android-36'), @('p37.zip','android-37'))) {
    $k = $p[0]
    try {
        Log "$k roots: $((Show-ZipRoot $files[$k]) -join ',')"
        Extract-Into $files[$k] "$ap\SDK\platforms"
        Log "platforms now: $((Get-ChildItem "$ap\SDK\platforms" -Directory | Select-Object -ExpandProperty Name) -join ', ')"
    } catch { Log "$k EXTRACT ERROR: $($_.Exception.Message)" }
}

# cmdline-tools -> SDK\cmdline-tools\16.0
try {
    Log "cmt roots: $((Show-ZipRoot $files['cmt.zip']) -join ',')"
    Extract-Into $files['cmt.zip'] "$ap\SDK\cmdline-tools"
    $ct = Get-ChildItem "$ap\SDK\cmdline-tools" -Directory | Where-Object { $_.Name -eq 'cmdline-tools' } | Select-Object -First 1
    if ($ct) { Get-ChildItem $ct.FullName | Move-Item -Destination "$ap\SDK\cmdline-tools\16.0" -Force; Remove-Item $ct.FullName -Recurse -Force -EA SilentlyContinue; Log "cmdline-tools renamed -> 16.0" }
    Log "sdkmanager: $(Test-Path "$ap\SDK\cmdline-tools\16.0\bin\sdkmanager.bat")"
} catch { Log "cmt EXTRACT ERROR: $($_.Exception.Message)" }

# --- 5) recap ---
Add-Content -Path $log -Value "--- recap ---"
foreach ($f in @(
    @("$ap\bin\HubInitializer.exe",'AP bin'),
    @("$ap\OpenJDK\bin\java.exe",'OpenJDK'),
    @("$ap\NDK\source.properties",'NDK'),
    @("$ap\SDK\platform-tools\adb.exe",'platform-tools'),
    @("$ap\SDK\build-tools\36.0.0\aapt2.exe",'build-tools'),
    @("$ap\SDK\cmdline-tools\16.0\bin\sdkmanager.bat",'cmdline-tools'),
    @("$ap\SDK\cmake\3.22.1\bin\cmake.exe",'cmake')
)) {
    $ok = if (Test-Path $f[0]) { 'OK' } else { 'MISSING' }
    Log "$($f[1]): $ok"
}

# --- 6) accept sdk licenses ---
try {
    $env:JAVA_HOME    = "$ap\OpenJDK"
    $env:ANDROID_HOME = "$ap\SDK"
    $sm = "$ap\SDK\cmdline-tools\16.0\bin\sdkmanager.bat"
    if (Test-Path $sm) {
        Log "accepting licenses..."
        $y = ("y`r`n") * 12
        $y | & $sm --licenses 2>>$log | Out-Null
        Log "licenses dir: $((Get-ChildItem "$ap\SDK\licenses" -EA SilentlyContinue | Select-Object -ExpandProperty Name) -join ', ')"
    }
} catch { Log "licenses ERROR: $($_.Exception.Message)" }

Add-Content -Path $log -Value "=== android module end ==="
