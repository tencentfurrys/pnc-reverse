$ErrorActionPreference = 'Continue'
$log = 'D:\Unity\fixup.log'
function Log($m) { "$(Get-Date -Format o) $m" | Out-File $log -Append -Encoding utf8 }

Log "=== fixup start ==="
$elevated = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Log "whoami: $(whoami) ; elevated: $elevated"

# 1. Remove the broken install tree (nested Editor\Editor)
Log "removing D:\Unity\Hub\Editor\6000.3.11f1 ..."
Remove-Item 'D:\Unity\Hub\Editor\6000.3.11f1' -Recurse -Force -ErrorAction SilentlyContinue
Log "exists after delete: $(Test-Path 'D:\Unity\Hub\Editor\6000.3.11f1')"

# 2. Reinstall to the correct parent path.
# The UnitySetup NSIS installer appends \Editor itself, so the final layout becomes:
#   D:\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe   <- exactly what Unity Hub expects
$exe = 'D:\Unity\installers\UnitySetup64-6000.3.11f1.exe'
Log "running installer: $exe /S /D=D:\Unity\Hub\Editor\6000.3.11f1"
$p = Start-Process $exe -ArgumentList '/S','/D=D:\Unity\Hub\Editor\6000.3.11f1' -Wait -PassThru
Log "installer exit code: $($p.ExitCode)"

# 3. Verify
$ok = Test-Path 'D:\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe'
Log "Unity.exe at expected path: $ok"
$top = (Get-ChildItem 'D:\Unity\Hub\Editor\6000.3.11f1' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name) -join ', '
Log "top-level of 6000.3.11f1: $top"
Log "=== fixup end ==="
