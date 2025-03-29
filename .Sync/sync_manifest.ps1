$src = Get-Content '.Sync\Packages\manifest.json' -Raw | ConvertFrom-Json
$dst = Get-Content '..\..\Packages\manifest.json' -Raw | ConvertFrom-Json

# Convert both to hashtables
$srcDeps = @{}
$src.dependencies.PSObject.Properties | ForEach-Object {
    $srcDeps[$_.Name] = $_.Value
}

$dstDeps = @{}
$dst.dependencies.PSObject.Properties | ForEach-Object {
    $dstDeps[$_.Name] = $_.Value
}

$changed = $false

foreach ($key in $srcDeps.Keys) {
    if (-not $dstDeps.ContainsKey($key)) {
        $dstDeps[$key] = $srcDeps[$key]
        Write-Host "Added: $key $($srcDeps[$key])"
        $changed = $true
    }
}

# Replace dependencies in dst with merged hashtable
$dst | Add-Member -MemberType NoteProperty -Name dependencies -Value $dstDeps -Force

if ($changed) {
    $dst | ConvertTo-Json -Depth 100 | Set-Content '..\..\Packages\manifest.json'
    Write-Host 'manifest.json updated.'
} else {
    Write-Host 'All packages already present.'
}