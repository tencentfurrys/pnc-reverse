$ErrorActionPreference = 'Stop'
$ap     = 'D:\Unity\Hub\Editor\6000.3.11f1\Editor\Data\PlaybackEngines\AndroidPlayer'
$sdkZip = 'D:\Unity\installers\sdk'
$log    = 'D:\Unity\android-module-repair.log'
Add-Content $log '=== repair2 start ==='
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Nuke($p) { if (Test-Path $p) { Remove-Item $p -Recurse -Force } }
function First-Dir($in) { Get-ChildItem $in | Select-Object -First 1 }

# ---- 1) cmdline-tools -> SDK\cmdline-tools\16.0 ----
Nuke "$ap\SDK\cmdline-tools"; Nuke "$ap\SDK\_t_cmt"
[IO.Compression.ZipFile]::ExtractToDirectory("$sdkZip\cmt.zip", "$ap\SDK\_t_cmt", $true)
$inner = First-Dir "$ap\SDK\_t_cmt"     # = cmdline-tools dir {bin,lib,...}
if ($inner.Name -eq 'cmdline-tools') {
    New-Item -ItemType Directory -Force "$ap\SDK\cmdline-tools" | Out-Null
    Move-Item $inner.FullName "$ap\SDK\cmdline-tools\16.0"
} else { Add-Content $log "cmt layout unexpected: $($inner.Name)" }
Nuke "$ap\SDK\_t_cmt"
Add-Content $log "cmdline-tools: $(Test-Path "$ap\SDK\cmdline-tools\16.0\bin\sdkmanager.bat")"

# ---- 2) cmake -> SDK\cmake\3.22.1 (zip roots are FLAT: bin,doc,share,source.properties) ----
Nuke "$ap\SDK\cmake"; Nuke "$ap\SDK\_t_cmk"
[IO.Compression.ZipFile]::ExtractToDirectory("$sdkZip\cmake.zip", "$ap\SDK\_t_cmk", $true)
New-Item -ItemType Directory -Force "$ap\SDK\cmake\3.22.1" | Out-Null
Get-ChildItem "$ap\SDK\_t_cmk" | Move-Item -Destination "$ap\SDK\cmake\3.22.1" -Force
Nuke "$ap\SDK\_t_cmk"
Add-Content $log "cmake: $(Test-Path "$ap\SDK\cmake\3.22.1\bin\cmake.exe")"

# ---- 3) build-tools -> SDK\build-tools\36.0.0 (zip root android-16) ----
Nuke "$ap\SDK\build-tools"; Nuke "$ap\SDK\_t_bt"
[IO.Compression.ZipFile]::ExtractToDirectory("$sdkZip\bt.zip", "$ap\SDK\_t_bt", $true)
$b = First-Dir "$ap\SDK\_t_bt"
if ($b.Name -eq 'android-16') {
    New-Item -ItemType Directory -Force "$ap\SDK\build-tools" | Out-Null
    Move-Item $b.FullName "$ap\SDK\build-tools\36.0.0"
} else { Add-Content $log "bt layout unexpected: $($b.Name)" }
Nuke "$ap\SDK\_t_bt"
Add-Content $log "aapt2: $(Test-Path "$ap\SDK\build-tools\36.0.0\aapt2.exe")"

# ---- 4) platform-tools: AndroidPlayer\platform-tools -> SDK\platform-tools ----
if (Test-Path "$ap\platform-tools") {
    Nuke "$ap\SDK\platform-tools"
    Move-Item "$ap\platform-tools" "$ap\SDK\platform-tools"
}
Add-Content $log "adb: $(Test-Path "$ap\SDK\platform-tools\adb.exe")"

# ---- 5) flat JDK items -> AndroidPlayer\OpenJDK ----
New-Item -ItemType Directory -Force "$ap\OpenJDK" | Out-Null
foreach ($n in @('bin','conf','include','legal','lib','NOTICE','release')) {
    $src = Join-Path $ap $n
    if (Test-Path $src) {
        $dst = Join-Path "$ap\OpenJDK" $n
        if (Test-Path $dst) {
            Get-ChildItem $src -Force | Move-Item -Destination $dst -Force
            Nuke $src
        } else { Move-Item $src $dst -Force }
    }
}
Add-Content $log "java.exe: $(Test-Path "$ap\OpenJDK\bin\java.exe")"

# ---- 6) recap ----
Add-Content $log '=== recap ==='
foreach ($f in @(
    @("$ap\OpenJDK\bin\java.exe",'OpenJDK+java'),
    @("$ap\NDK\source.properties",'NDK'),
    @("$ap\SDK\platform-tools\adb.exe",'platform-tools'),
    @("$ap\SDK\build-tools\36.0.0\aapt2.exe",'build-tools'),
    @("$ap\SDK\cmdline-tools\16.0\bin\sdkmanager.bat",'cmdline-tools'),
    @("$ap\SDK\cmake\3.22.1\bin\cmake.exe",'cmake'),
    @("$ap\Tools",'Tools')
)) {
    $state = if (Test-Path $f[0]) {
        if ((Get-Item $f[0]).PSIsContainer) { "OK-dir [$( (Get-ChildItem $f[0] | Select-Object -First 10 -ExpandProperty Name) -join ', ')]" } else { 'OK' }
    } else { 'MISSING' }
    Add-Content $log "$($f[1]): $state"
}

# ---- 7) accept SDK licenses ----
try {
    $env:JAVA_HOME    = "$ap\OpenJDK"
    $env:ANDROID_HOME = "$ap\SDK"
    $sm = "$ap\SDK\cmdline-tools\16.0\bin\sdkmanager.bat"
    if (Test-Path $sm) {
        Add-Content $log 'accepting licenses...'
        $ys = ('y' + [char]13 + [char]10) * 15
        $ys | & $sm --licenses 2>&1 | Select-Object -Last 8 | ForEach-Object { Add-Content $log "lic: $_" }
        Add-Content $log "licenses: $((Get-ChildItem "$ap\SDK\licenses" -EA SilentlyContinue | Select-Object -ExpandProperty Name) -join ', ')"
    } else { Add-Content $log 'sdkmanager missing; skip licenses' }
} catch { Add-Content $log "licenses ERROR: $($_.Exception.Message)" }

Add-Content $log '=== repair2 end ==='
