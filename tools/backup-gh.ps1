$ErrorActionPreference = 'Continue'
$tok = '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'
$stage = 'D:\PNC\backup-gh\pnc-reverse'

function Robo($src, $dst, $expo) {
    $rc = robocopy $src $dst /E /MT:8 /R:1 /W:1 /NFL /NDL /NP /NJH /NJS @expo
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed: $src -> $dst (exit $LASTEXITCODE)" }
    Write-Output "copied : $dst  (robocopy exit $LASTEXITCODE)"
}

New-Item -ItemType Directory -Force -Path $stage | Out-Null
Robo 'D:\PNC\exported\ExportedProject' "$stage\ExportedProject" @('/XD', 'Library', 'Temp', 'Obj', 'Logs', 'Build', 'obj', '.vs', 'UpgradeBackup')
Robo 'D:\PNC\decomp' "$stage\decomp\Assembly-CSharp" @('/XD', 'obj', 'BinObj', '.vs')
Robo 'D:\PNC\exported\AuxiliaryFiles' "$stage\game-data-aux" @()
Robo 'D:\Unity' "$stage\tools" @('/XD', 'installers', 'Hub', 'fixup.ps1', '*.log')

# gitignore for the staging repo
@"
# Unity build caches (regenerable)
ExportedProject/Library/
ExportedProject/Temp/
ExportedProject/Obj/
ExportedProject/Logs/
ExportedProject/obj/
ExportedProject/Build/
[Bb]uilds/
*.apk

# editor junk
ExportedProject/.vs/
ExportedProject/.idea/
*.csproj.user
*.suo
"D:\Tools\pnc-rev"
ExportedProject/Temp/
"@ | Set-Content "$stage\.gitignore" -Encoding UTF8

Push-Location $stage
git init -b main 2>&1 | Out-Null
git config core.longpaths true
git config user.name "Bradley Adams"
git config user.email "bradleyadamswork@gmail.com"
git add -A 2>&1 | Select-Object -Last 2
git commit -m "Backup: PNC 0.3.1 reverse engineering - exported Unity project (6000.3.11f1), decompiledAssembly-CSharp, path-id map, tooling" 2>&1 | Select-Object -Last 1
Pop-Location

# --- GitHub: identity + create private repo ---
$me = Invoke-RestMethod -Uri 'https://api.github.com/user' -Headers @{ Authorization = "Bearer $tok"; Accept = 'application/vnd.github+json' }
Write-Output "gh login : $($me.login)"
$body = @{ name = 'pnc-reverse'; private = $true; description = 'Reverse-engineering backup of Post Nut Calamity 0.3.1 (Unity 6000.3.11f1 rip)' } | ConvertTo-Json
$rep = Invoke-RestMethod -Method Post -Uri 'https://api.github.com/user/repos' -Headers @{ Authorization = "Bearer $tok"; Accept = 'application/vnd.github+json' } -ContentType 'application/json' -Body $body
Write-Output "repo     : $($rep.full_name) private=$($rep.private)"

git -C $stage remote add origin "https://$([uri]::EscapeDataString($tok))@github.com/$($rep.full_name).git" 2>&1
Write-Output "remote set. ready to push"
