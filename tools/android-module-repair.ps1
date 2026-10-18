$ErrorActionPreference = 'Stop'
$ap     = 'D:\Unity\Hub\Editor\6000.3.11f1\Editor\Data\PlaybackEngines\AndroidPlayer'
$sdkZip = 'D:\Unity\installers\sdk'
$log    = 'D:\Unity\android-module-repair.log'
Add-Content $log '=== repair start ==='
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Extract-Fresh($zip, $dest) {
    $z = [System.IO.Compression.ZipFile]::OpenRead($zip)
    $roots = ($z.Entries | ForEach-Object { ($_.FullName -split '/')[0] } | Sort-Object -Unique) -join ','
    $z.Dispose()
    Add-Content $log "roots of $(Split-Path $zip -Leaf): $roots"
    ExtractTo $zip $dest $roots
}
function ExtractTo($zip, $dest, $roots) {
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zip, $dest, $true)
}

# 1) cmdline-tools fresh: want SDK\cmdline-tools\16.0\{bin,lib,source.properties}
Remove-Item "$ap\SDK\cmdline-tools" -Recurse -Force -EA SilentlyContinue
[IO.Compression.ZipFile]::ExtractToDirectory("$sdkZip\cmt.zip", "$ap\SDK\cmdline-tools-tmp", $true)
$inner = Get-ChildItem "$ap\SDK\cmdline-tools-tmp" | Where-Object Name -match 'cmdline' | Select-Object -First 1
if ($inner) {
    Move-Item $inner.FullName "$ap\SDK\cmdline-tools" -Force
} else {
    New-Item -ItemType Directory -Force "$ap\SDK\cmdline-tools" | Out-Null
    Move-Item "$ap\SDK\cmdline-tools-tmp\*" "$ap\SDK\cmdline-tools\" -Force
}
Remove-Item "$ap\SDK\cmdline-tools-tmp" -Recurse -Force -EA SilentlyContinue
Move-Item "$ap\SDK\cmdline-tools" "$ap\SDK\cmdline-tools-real" -Force
Move-Item "$ap\SDK\cmdline-tools-real" "$ap\SDK\cmdline-tools" -Force   # no-op sanity; ensure clean state
# now rename inner only if nested: cmdline-tools\cmdline-tools -> 16.0
if (Test-Path "$ap\SDK\cmdline-tools\cmdline-tools") {
    Move-Item "$ap\SDK\cmdline-tools\cmdline-tools" "$ap\SDK\cmdline-tools\16.0"
}
Add-Content $log "cmdline-tools 16.0: $(Test-Path "$ap\SDK\cmdline-tools\16.0\bin\sdkmanager.bat")"

# 2) cmake fresh: want SDK\cmake\3.22.1\{bin,...}
Remove-Item "$ap\SDK\cmake" -Recurse -Force -EA SilentlyContinue
[IO.Compression.ZipFile]::ExtractToDirectory("$sdkZip\cmake.zip", "$ap\SDK\cmake-tmp", $true)
if (Test-Path "$ap\SDK\cmake-tmp\cmake") {   # zip root folder named cmake (typical)
    Move-Item "$ap\SDK\cmake-tmp\cmake" "$ap\SDK\3.22.1-dir"
    Move-Item "$ap\SDK\3.22.1-dir" "$ap\SDK\cmake" -Force
} else {
    New-Item -ItemType Directory -Force "$ap\SDK\cmake" | Out-Null
    Move-Item "$ap\SDK\cmake-tmp\*" "$ap\SDK\cmake\" -Force
}
Remove-Item "$ap\SDK\cmake-tmp" -Recurse -Force -EA SilentlyContinue
if (Test-Path "$ap\SDK\cmake\cmake") { Move-Item "$ap\SDK\cmake\cmake" "$ap\SDK\cmake\.r1" }
# flatten: everything under SDK\cmake should sit in 3.22.1
$src = $null
if (Test-Path "$ap\SDK\cmake\.r1") { $src = "$ap\SDK\cmake\.r1" } else { $src = "$ap\SDK\cmake" }
New-Item -ItemType Directory -Force "$ap\SDK\cmake\3.22.1" | Out-Null
if ($src -ne "$ap\SDK\cmake\3.22.1") {
    Get-ChildItem $src | Move-Item -Destination "$ap\SDK\cmake\3.22.1\" -Force -EA SilentlyContinue
}
if (Test-Path "$ap\SDK\cmake\.r1") { Remove-Item "$ap\SDK\cmake\.r1" -Recurse -Force -EA SilentlyContinue }
Add-Content $log "cmake 3.22.1: $(Test-Path "$ap\SDK\cmake\3.22.1\bin\cmake.exe")"

# 3) build-tools fresh: want SDK\build-tools\36.0.0\{aapt2.exe, lib, ...}
Remove-Item "$ap\SDK\build-tools" -Recurse -Force -EA SilentlyContinue
[IO.Compression.ZipFile]::ExtractToDirectory("$sdkZip\bt.zip", "$ap\SDK\build-tools-tmp", $true)
# zip root = android-16 -> rename to 36.0.0 directly (this was already fine before, redo cleanly)
Move-Item "$ap\SDK\build-tools-tmp\*" "$ap\SDK\build-tools-tmp-root" -Force -EA SilentlyContinue
Remove-Item "$ap\SDK\build-tools-tmp" -Recurse -Force -EA SilentlyContinue
Move-Item "$ap\SDK\build-tools-tmp-root" "$ap\SDK\build-tools" -Force
if (Test-Path "$ap\SDK\build-tools\android-16") { Move-Item "$ap\SDK\build-tools\android-16" "$ap\SDK\build-tools\36.0.0" }
Add-Content $log "build-tools 36.0.0 aapt2: $(Test-Path "$ap\SDK\build-tools\36.0.0\aapt2.exe")"
Add-Content $log "build-tools 36.0.0 d8:    $(Test-Path "$ap\SDK\build-tools\36.0.0\d8.bat")"
Add-Content $log "build-tools listing: $((Get-ChildItem "$ap\SDK\build-tools\36.0.0" | Select-Object -ExpandProperty Name -First 12) -join ', ')"

# 4) platform-tools: exists at AndroidPlayer\platform-tools -> move into SDK
if (Test-Path "$ap\platform-tools") {
    if (Test-Path "$ap\SDK\platform-tools") { Remove-Item "$ap\SDK\platform-tools" -Recurse -Force }
    Move-Item "$ap\platform-tools" "$ap\SDK\platform-tools"
}
Add-Content $log "adb: $(Test-Path "$ap\SDK\platform-tools\adb.exe")"

# 5) JDK: loose items in AndroidPlayer root -> OpenJDK
New-Item -ItemType Directory -Force "$ap\OpenJDK" | Out-Null
foreach ($n in @('bin','conf','include','legal','lib','NOTICE','release')) {
    $src = Join-Path $ap $n
    if (Test-Path $src) {
        $dst = Join-Path "$ap\OpenJDK" $n
        if (Test-Path $dst) {
            Get-ChildItem $src -Force | Move-Item -Destination $dst -Force
            Remove-Item $src -Recurse -Force -EA SilentlyContinue
        } else {
            Move-Item $src $dst -Force
        }
    }
}
Add-Content $log "java.exe: $(Test-Path "$ap\OpenJDK\bin\java.exe")"

# 6) recap
Add-Content $log '=== recap ==='
foreach ($f in @(
    @("$ap\OpenJDK\bin\java.exe",'OpenJDK'),
    @("$ap\NDK\source.properties",'NDK'),
    @("$ap\SDK\platform-tools\adb.exe",'platform-tools'),
    @("$ap\SDK\build-tools\36.0.0\aapt2.exe",'build-tools'),
    @("$ap\SDK\cmdline-tools\16.0\bin\sdkmanager.bat",'cmdline-tools'),
    @("$ap\SDK\cmake\3.22.1\bin\cmake.exe",'cmake'),
    @("$ap\Tools\gradle",'gradle(Tools)'),
    @("$ap\SDK\platforms",'platforms')
)) {
    $state = if (Test-Path $f[0]) {
        if ((Get-Item $f[0]).PSIsContainer) { "OK-dir ($((Get-ChildItem $f[0] | Select-Object -First 8 -ExpandProperty Name) -join ','))" } else { 'OK' }
    } else { 'MISSING' }
    Add-Content $log "$($f[1]): $state"
}
Add-Content $log '=== repair end ==='
