param(
    [ValidateSet('Prepare', 'Tests', 'Review', 'Windows', 'Android')]
    [string]$Action = 'Tests',
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.12f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not (Test-Path -LiteralPath $Unity -PathType Leaf)) { throw "Unity executable not found: $Unity" }
$evidenceRoot = Join-Path $projectRoot 'Docs\Evidence'
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$log = Join-Path $evidenceRoot "unity-$($Action.ToLower())-$stamp.log"
# Start-Process joins its argument list, so quote the two potentially spaced paths.
$arguments = @('-batchmode', '-projectPath', ('"' + $projectRoot + '"'), '-logFile', ('"' + $log + '"'))
if ($Action -in @('Tests', 'Review')) {
    $testResult = Join-Path $evidenceRoot "editmode-$($Action.ToLower())-$stamp.xml"
    # A loose namespace also matches PixelFlow.Tests.Editor.dll and selects all
    # descendants. Match the rule fixtures explicitly so -nographics stays valid.
    $filter = if ($Action -eq 'Review') { '^PixelFlow\.EditorReview\.' } else { '^PixelFlow\.Tests\.(PuzzleSessionTests|CampaignTests|FullGameplayTests|ProgressFlowTests)\.' }
    # Rules/save checks stay usable with no graphics device. The scene review
    # enters Play Mode inside the Editor and renders previews; it never builds.
    if ($Action -eq 'Tests') { $arguments += '-nographics' }
    $arguments += @('-runTests', '-testPlatform', 'EditMode', '-testFilter', ('"' + $filter + '"'), '-testResults', ('"' + $testResult + '"'))
    Write-Output "Editor-only $Action results: $testResult"
} else {
    $method = if ($Action -eq 'Prepare') { 'Prepare' } else { $Action }
    $arguments += @('-quit', '-executeMethod', "PixelFlow.Editor.PixelFlowBuild.$method")
}
$previousReviewRun = $env:PIXEL_FLOW_REVIEW_RUN
try {
    if ($Action -eq 'Review' -and [string]::IsNullOrWhiteSpace($previousReviewRun)) {
        $env:PIXEL_FLOW_REVIEW_RUN = "review-$stamp"
    }
    $unityProcess = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
} finally {
    $env:PIXEL_FLOW_REVIEW_RUN = $previousReviewRun
}
Write-Output "Unity $Action PID $($unityProcess.Id); log $log"
$unityProcess.WaitForExit()
if ($unityProcess.ExitCode -eq 0 -and $Action -in @('Tests', 'Review')) {
    if (-not (Test-Path -LiteralPath $testResult)) { throw 'Unity exited without test results' }
    [xml]$results = Get-Content -LiteralPath $testResult -Raw
    if ([int]$results.'test-run'.total -lt 1 -or [int]$results.'test-run'.failed -gt 0) { throw 'Unity produced empty or failed test results' }
    foreach ($case in $results.SelectNodes('//test-case')) {
        $expectedPrefix = if ($Action -eq 'Review') { 'PixelFlow.EditorReview.' } else { 'PixelFlow.Tests.' }
        if (-not $case.fullname.StartsWith($expectedPrefix)) { throw "Unexpected test selected: $($case.fullname)" }
    }
    Write-Output "$Action verified $($results.'test-run'.passed)/$($results.'test-run'.total) cases."
}
exit $unityProcess.ExitCode
