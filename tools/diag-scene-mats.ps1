# Diagnostic: list materials referenced (directly or via one prefab hop) by a scene,
# and the shader each binds. Usage: diag-scene-mats.ps1 <scene-or-prefab-path>
param([string]$Root = "ExportedProject\Assets\Scenes\MainMenu.unity")

$ErrorActionPreference = "Stop"

# guid -> file map for asset types we care about
$map = @{}
Get-ChildItem ExportedProject\Assets -Recurse -Include *.mat,*.prefab,*.unity -File | ForEach-Object {
    $meta = "$($_.FullName).meta"
    if (Test-Path $meta) {
        $g = (Select-String -Path $meta -Pattern 'guid: ([0-9a-f]{32})' | Select-Object -First 1).Matches[0].Groups[1].Value
        $map[$g] = $_.FullName
    }
}

function Get-ReferencedMaterialPaths([string]$file) {
    $refs = Select-String -Path $file -Pattern 'guid: ([0-9a-f]{32})' -AllMatches |
        ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
    $mats = @()
    foreach ($r in $refs) {
        if ($map.ContainsKey($r) -and $map[$r] -match '\.mat$' -and $map[$r] -ne $file) { $mats += $map[$r] }
    }
    return $mats
}

$seen = @{}
$queue = New-Object System.Collections.Queue
$queue.Enqueue($Root)

"=== traversal from $Root ==="
while ($queue.Count -gt 0) {
    $cur = $queue.Dequeue()
    $mats = Get-ReferencedMaterialPaths $cur
    foreach ($m in $mats) {
        # shader the material binds
        $sg = (Select-String -Path $m -Pattern 'm_Shader: \{fileID: \d+, guid: ([0-9a-f]{32})' | Select-Object -First 1).Matches[0].Groups[1].Value
        $target = $map[$sg]
        if (-not $target) { $target = 'MISSING:' + $sg }
        Write-Output ("{0,55} => {1}" -f (Split-Path $m -Leaf), (Split-Path $target -Leaf))
        # follow prefabs to discover nested material usage
        $refs = Select-String -Path $cur -Pattern 'guid: ([0-9a-f]{32})' -AllMatches |
            ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value }
        foreach ($r in $refs) {
            if ($map.ContainsKey($r) -and $map[$r] -match '\.prefab$' -and -not $seen[$r]) {
                $seen[$r] = $true
                $queue.Enqueue($map[$r])
            }
        }
    }
}
