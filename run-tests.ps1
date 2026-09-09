param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe',
    [ValidateSet('All', 'EditMode', 'PlayMode')][string]$Mode = 'All',
    [int]$RandomOrderSeed = 0
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity 6000.3.11f1 was not found at $UnityPath. Pass -UnityPath with the actual Editor executable."
}
$projectRoot = $PSScriptRoot
$runDirectory = Join-Path $projectRoot ('TestResults/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
$modes = if ($Mode -eq 'All') { @('EditMode', 'PlayMode') } else { @($Mode) }
$anyFailures = $false
foreach ($testMode in $modes) {
    $resultPath = Join-Path $runDirectory "$testMode.xml"
    $logPath = Join-Path $runDirectory "$testMode.log"
    $arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $projectRoot + '"'),
        '-runTests', '-testPlatform', $testMode, '-testResults', ('"' + $resultPath + '"'),
        '-logFile', ('"' + $logPath + '"'))
    if ($RandomOrderSeed -ne 0) { $arguments += @('-randomOrderSeed', "$RandomOrderSeed") }
    # Do not pass -quit: the test runner exits after completion itself.
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if (-not (Test-Path -LiteralPath $resultPath)) {
        throw "No XML result for $testMode (exit $($process.ExitCode)); inspect $logPath"
    }
    [xml]$results = Get-Content -LiteralPath $resultPath -Raw
    $run = $results.'test-run'
    if (-not $run -or [int]$run.total -eq 0) { throw "Empty/invalid results: $resultPath" }
    Write-Host "$testMode : total=$($run.total), passed=$($run.passed), failed=$($run.failed), skipped=$($run.skipped), exit=$($process.ExitCode)"
    if ([int]$run.failed -gt 0) { $anyFailures = $true }
    elseif ($process.ExitCode -ne 0) { throw "Unexpected Editor exit $($process.ExitCode); inspect $logPath" }
}
Write-Host "Results: $runDirectory"
if ($anyFailures) { exit 2 }
exit 0
