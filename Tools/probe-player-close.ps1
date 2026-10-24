param([string]$RunId = 'manual-probe')
$ErrorActionPreference = 'Stop'
if ($RunId -notmatch '^[A-Za-z0-9_-]{1,64}$') { throw 'Invalid run ID' }
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$executable = Join-Path $projectRoot 'Builds\Windows\PixelFlowClone.exe'
$log = Join-Path $projectRoot "Docs\Evidence\player-close-$RunId.log"
$report = Join-Path $projectRoot "Docs\Evidence\player-close-$RunId.json"
$arguments = @('-screen-fullscreen','0','-screen-width','540','-screen-height','960','-pixelFlowSaveScope',$RunId,'-logFile',('"'+$log+'"'))
$playerProcess = Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Normal -PassThru
Write-Output "Normal Home/close probe PID: $($playerProcess.Id)"
$null = $playerProcess.WaitForExit(6000)
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class PixelFlowCloseProbe {
 public delegate bool EnumProc(IntPtr window, IntPtr arg);
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback, IntPtr arg);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
 public static void Close(uint target) { EnumWindows((window,arg)=>{ uint pid; GetWindowThreadProcessId(window,out pid); if(pid==target) PostMessage(window,0x10,IntPtr.Zero,IntPtr.Zero); return true;},IntPtr.Zero); }
}
'@
if (-not $playerProcess.HasExited) { [PixelFlowCloseProbe]::Close($playerProcess.Id) }
$timedOut = -not $playerProcess.WaitForExit(15000)
if ($timedOut) { $playerProcess.Kill(); $playerProcess.WaitForExit() }
$result = [ordered]@{status=if(-not $timedOut -and $playerProcess.ExitCode -eq 0){'pass'}else{'fail'}; exitCode=$playerProcess.ExitCode; timedOut=$timedOut; scope='Visible normal Home launch, then WM_CLOSE; no development smoke, injected pointer or gameplay shortcut.'}
$result | ConvertTo-Json | Set-Content -LiteralPath $report -Encoding UTF8
$result | ConvertTo-Json
exit $(if($result.status -eq 'pass'){0}else{1})
