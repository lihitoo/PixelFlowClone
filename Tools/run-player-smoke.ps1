param([string]$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss'))
$ErrorActionPreference = 'Stop'
if ($RunId -notmatch '^[A-Za-z0-9_-]{1,64}$') { throw 'Invalid run ID' }
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$executable = Join-Path $projectRoot 'Builds\Windows\PixelFlowClone.exe'
$evidenceRoot = Join-Path $projectRoot "Docs\Evidence\player-$RunId"
if (Test-Path -LiteralPath $evidenceRoot) { throw 'Evidence destination already exists; select a fresh run ID.' }
New-Item -ItemType Directory -Path $evidenceRoot | Out-Null
$log = Join-Path $evidenceRoot 'player.log'
$sourceHash = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant()
$buildRoot = Split-Path -Parent $executable
$artifactFiles = @(Get-ChildItem -LiteralPath $buildRoot -Recurse -File | Sort-Object FullName | ForEach-Object {
    [ordered]@{path=$_.FullName.Substring($buildRoot.Length+1).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
})
$manifestJson = $artifactFiles | ConvertTo-Json -Depth 4 -Compress
$hasher = [System.Security.Cryptography.SHA256]::Create()
try { $artifactFingerprint = -join ($hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($manifestJson)) | ForEach-Object { $_.ToString('x2') }) } finally { $hasher.Dispose() }
$manifestJson | Set-Content -LiteralPath (Join-Path $evidenceRoot 'artifact-files.json') -Encoding UTF8
$arguments = @('-screen-fullscreen','0','-screen-width','540','-screen-height','960','-pixelFlowSmoke','-pixelFlowSaveScope',$RunId,'-pixelFlowEvidence',('"'+$evidenceRoot+'"'),'-logFile',('"'+$log+'"'))
# Real frame captures require a visible player window. The pointer device is virtual
# inside Unity; this runner does not move or click the user's OS mouse.
$playerProcess = Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Normal -PassThru
Write-Output "Visible player smoke PID: $($playerProcess.Id)"
$playerProcess.Id | Set-Content -LiteralPath (Join-Path $evidenceRoot 'player.pid')
$timedOut = -not $playerProcess.WaitForExit(220000)
if ($timedOut) { $playerProcess.Kill(); $playerProcess.WaitForExit() }
$failures = [System.Collections.Generic.List[string]]::new()
if ($timedOut) { $failures.Add('Player scenario timed out.') }
if ($playerProcess.ExitCode -ne 0) { $failures.Add("Process exit code: $($playerProcess.ExitCode)") }
$scenarioPath = Join-Path $evidenceRoot 'smoke.json'
if (-not (Test-Path -LiteralPath $scenarioPath)) { $failures.Add('Scenario did not produce a result.') }
else {
    $scenario = Get-Content -Raw -LiteralPath $scenarioPath | ConvertFrom-Json
    if ($scenario.status -ne 'pass') { $failures.Add('Scenario reported failure.') }
}
Add-Type -AssemblyName System.Drawing
$captures = @()
foreach ($name in @('home.png','level-1.png','win.png','home-progress.png')) {
    $path = Join-Path $evidenceRoot $name
    if (-not (Test-Path -LiteralPath $path)) { $failures.Add("Missing capture: $name"); continue }
    $bitmap = [System.Drawing.Bitmap]::new($path)
    try {
        $colors = [System.Collections.Generic.HashSet[int]]::new()
        for ($row=1; $row -lt 12; $row++) {
            for ($column=1; $column -lt 8; $column++) {
                $null = $colors.Add($bitmap.GetPixel([int]($bitmap.Width*$column/8),[int]($bitmap.Height*$row/12)).ToArgb())
            }
        }
        $captures += [ordered]@{file=$name;width=$bitmap.Width;height=$bitmap.Height;sampledColors=$colors.Count;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
        if ($colors.Count -lt 8) { $failures.Add("Capture is blank or nearly solid: $name") }
    } finally { $bitmap.Dispose() }
}
$receipt = [ordered]@{status=if($failures.Count -eq 0){'pass'}else{'fail'};exitCode=$playerProcess.ExitCode;timedOut=$timedOut;executableSha256=$sourceHash;artifactFingerprint=$artifactFingerprint;artifactFiles=$artifactFiles.Count;failures=$failures.ToArray();captures=$captures;scope='Scenario checks plus real process exit and nonblank frame identity. Visual fidelity, listening, device performance and human acceptance are separate.'}
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'runner.json') -Encoding UTF8
$receipt | ConvertTo-Json -Depth 6
exit $(if($receipt.status -eq 'pass'){0}else{1})
