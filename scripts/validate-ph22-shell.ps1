[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Executable,
    [string]$EvidencePath,
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [int]$StartupSeconds = 8
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

if (-not ('MediaForgeShellValidation.NativeWindow' -as [type])) {
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

namespace MediaForgeShellValidation
{
    public static class NativeWindow
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
"@
}

$ResolvedExecutable = (Resolve-Path -LiteralPath $Executable).Path
$ResolvedProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path

if (-not $EvidencePath) {
    $EvidenceRoot = Join-Path $ResolvedProjectRoot (
        'artifacts\ph22-shell-validation-' +
        (Get-Date -Format 'yyyyMMdd-HHmmss')
    )
    $EvidencePath = Join-Path $EvidenceRoot 'ph22-shell-validation.json'
}

$script:Checks = @()
$script:Stage = 'initialise'
$Failure = $null
$Process = $null
$IsolationRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    'MediaForge-PH22-Shell-' + [Guid]::NewGuid().ToString('N')
)

function Set-Stage {
    param([Parameter(Mandatory=$true)][string]$Name)
    $script:Stage = $Name
    Write-Host "STAGE: $Name"
}

function Add-Check {
    param(
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$true)][bool]$Passed,
        [Parameter(Mandatory=$true)][string]$Detail
    )

    $script:Checks += [pscustomobject]@{
        name = $Name
        passed = $Passed
        detail = $Detail
    }

    if ($Passed) {
        Write-Host "PASS: $Name - $Detail"
    }
    else {
        throw "FAIL: $Name - $Detail"
    }
}

function Stop-ProcessTree {
    param([System.Diagnostics.Process]$Target)

    if ($null -eq $Target -or $Target.HasExited) {
        return
    }

    try {
        $Target.Kill($true)
        $Target.WaitForExit(5000) | Out-Null
    }
    catch {
        & taskkill.exe /PID $Target.Id /T /F 2>&1 | Out-Null
    }
}

function Get-AllElements {
    param([System.Windows.Automation.AutomationElement]$Root)

    $Collection = $Root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition
    )

    for ($Index = 0; $Index -lt $Collection.Count; $Index++) {
        $Collection.Item($Index)
    }
}

function Find-ByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [string]$ControlTypeName
    )

    foreach ($Element in @(Get-AllElements -Root $Root)) {
        try {
            if ([string]$Element.Current.Name -ne $Name) {
                continue
            }

            if (
                $ControlTypeName -and
                [string]$Element.Current.ControlType.ProgrammaticName -ne $ControlTypeName
            ) {
                continue
            }

            return $Element
        }
        catch {
        }
    }

    return $null
}

function Wait-ByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [string]$ControlTypeName,
        [int]$TimeoutMilliseconds = 3000
    )

    $Deadline = [DateTime]::UtcNow.AddMilliseconds($TimeoutMilliseconds)

    do {
        $Element = Find-ByName `
            -Root $Root `
            -Name $Name `
            -ControlTypeName $ControlTypeName

        if ($null -ne $Element) {
            return $Element
        }

        Start-Sleep -Milliseconds 100
    }
    while ([DateTime]::UtcNow -lt $Deadline)

    return $null
}

function Get-ControlNames {
    param([System.Windows.Automation.AutomationElement]$Root)

    foreach ($Element in @(Get-AllElements -Root $Root)) {
        try {
            $Name = [string]$Element.Current.Name
            if (-not [string]::IsNullOrWhiteSpace($Name)) {
                $Name
            }
        }
        catch {
        }
    }
}

function Focus-App {
    param([System.Diagnostics.Process]$Target)

    $Target.Refresh()

    $Handle = [IntPtr]$Target.MainWindowHandle

    if ($Handle -eq [IntPtr]::Zero) {
        throw 'Main window handle is unavailable.'
    }

    [MediaForgeShellValidation.NativeWindow]::ShowWindow(
        $Handle,
        5
    ) | Out-Null

    [MediaForgeShellValidation.NativeWindow]::SetForegroundWindow(
        $Handle
    ) | Out-Null

    Start-Sleep -Milliseconds 150
}

function Send-Key {
    param(
        [System.Diagnostics.Process]$Target,
        [string]$Keys
    )

    Focus-App -Target $Target
    [System.Windows.Forms.SendKeys]::SendWait($Keys)
    Start-Sleep -Milliseconds 250
}

function Get-ExpandState {
    param([System.Windows.Automation.AutomationElement]$Element)

    try {
        $Pattern = [System.Windows.Automation.ExpandCollapsePattern](
            $Element.GetCurrentPattern(
                [System.Windows.Automation.ExpandCollapsePattern]::Pattern
            )
        )

        return $Pattern.Current.ExpandCollapseState
    }
    catch {
        return $null
    }
}

function Assert-MenuKeyboardAccess {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [System.Diagnostics.Process]$Target,
        [string]$Name,
        [string]$AccessKey
    )

    $Menu = Find-ByName `
        -Root $Root `
        -Name $Name `
        -ControlTypeName 'ControlType.MenuItem'

    if ($null -eq $Menu) {
        throw "Menu '$Name' was not found."
    }

    if (-not $Menu.Current.IsKeyboardFocusable) {
        throw "Menu '$Name' is not keyboard focusable."
    }

    $Expanded = $false
    $ObservedStates = @()

    for ($Attempt = 1; $Attempt -le 3; $Attempt++) {
        Focus-App -Target $Target

        [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
        Start-Sleep -Milliseconds 120

        Focus-App -Target $Target
        [System.Windows.Forms.SendKeys]::SendWait(("%$AccessKey"))
        Start-Sleep -Milliseconds (250 + (100 * $Attempt))

        $Menu = Find-ByName `
            -Root $Root `
            -Name $Name `
            -ControlTypeName 'ControlType.MenuItem'

        $State = if ($null -eq $Menu) {
            $null
        }
        else {
            Get-ExpandState -Element $Menu
        }

        $StateText = if ($null -eq $State) {
            'Unavailable'
        }
        else {
            [string]$State
        }

        $ObservedStates += "attempt $Attempt=$StateText"

        if (
            $null -ne $State -and
            $State -eq [System.Windows.Automation.ExpandCollapseState]::Expanded
        ) {
            $Expanded = $true
            break
        }

        Start-Sleep -Milliseconds 150
    }

    if (-not $Expanded) {
        throw "Menu '$Name' did not expand through Alt+$($AccessKey.ToUpperInvariant()) after 3 foregrounded attempts. Observed: $($ObservedStates -join '; ')."
    }

    [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
    Start-Sleep -Milliseconds 150

    Write-Host "  keyboard menu '$Name': $($ObservedStates -join '; ')"
}

$StartedUtc = [DateTime]::UtcNow
$MainWindowTitle = ''
$MainWindowHandle = 0
$HighContrastAtRun = [System.Windows.Forms.SystemInformation]::HighContrast
$ScreenBounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds

try {
    Set-Stage 'static-system-resources'

    $AppXamlPath = Join-Path $ResolvedProjectRoot 'App.xaml'
    $AppXaml = Get-Content -LiteralPath $AppXamlPath -Raw -Encoding UTF8

    foreach ($RequiredResource in @(
        'SystemColors.HighlightColorKey',
        'SystemColors.WindowColorKey',
        'SystemColors.ControlColorKey',
        'SystemColors.ControlDarkColorKey',
        'SystemColors.GrayTextColorKey',
        'SystemColors.HighlightTextBrushKey'
    )) {
        if ($AppXaml.IndexOf($RequiredResource, [StringComparison]::Ordinal) -lt 0) {
            throw "System-aware App.xaml resource is missing: $RequiredResource"
        }
    }

    Add-Check `
        -Name 'system-aware-resources' `
        -Passed $true `
        -Detail 'App.xaml semantic brushes use dynamic WPF SystemColors keys.'

    Set-Stage 'isolated-profile'

    $AppData = Join-Path $IsolationRoot 'AppData\Roaming'
    $LocalAppData = Join-Path $IsolationRoot 'AppData\Local'
    New-Item -ItemType Directory -Path $AppData, $LocalAppData -Force | Out-Null

    Set-Stage 'process-start'

    $StartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $StartInfo.FileName = $ResolvedExecutable
    $StartInfo.WorkingDirectory = Split-Path -Parent $ResolvedExecutable
    $StartInfo.UseShellExecute = $false
    $StartInfo.EnvironmentVariables['APPDATA'] = [string]$AppData
    $StartInfo.EnvironmentVariables['LOCALAPPDATA'] = [string]$LocalAppData
    $StartInfo.EnvironmentVariables['MEDIAFORGE_APPDATA_ROOT'] = [string]$AppData
    $StartInfo.EnvironmentVariables['MEDIAFORGE_LOCALAPPDATA_ROOT'] = [string]$LocalAppData
    $StartInfo.EnvironmentVariables['MEDIAFORGE_LAUNCH_SMOKE'] = '1'

    $Process = New-Object System.Diagnostics.Process
    $Process.StartInfo = $StartInfo

    if (-not $Process.Start()) {
        throw 'Process.Start returned false.'
    }

    try {
        $Process.WaitForInputIdle($StartupSeconds * 1000) | Out-Null
    }
    catch {
    }

    Set-Stage 'wait-main-window'

    $Deadline = [DateTime]::UtcNow.AddSeconds($StartupSeconds)

    while ([DateTime]::UtcNow -lt $Deadline) {
        if ($Process.HasExited) {
            throw "Process exited during startup with code $($Process.ExitCode)."
        }

        $Process.Refresh()

        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) {
            break
        }

        Start-Sleep -Milliseconds 100
    }

    $Process.Refresh()

    $Handle = [IntPtr]$Process.MainWindowHandle

    if ($Handle -eq [IntPtr]::Zero) {
        throw 'No top-level MediaForge window appeared.'
    }

    $MainWindowHandle = $Handle.ToInt64()
    $MainWindowTitle = [string]$Process.MainWindowTitle

    Set-Stage 'automation-root'

    $Root = [System.Windows.Automation.AutomationElement]::FromHandle($Handle)

    if ($null -eq $Root) {
        throw 'UI Automation could not resolve the MediaForge main window.'
    }

    $LaunchTitleValid =
        $MainWindowTitle.StartsWith(
            'MediaForge',
            [System.StringComparison]::Ordinal
        ) -and
        $MainWindowTitle.IndexOf(
            'Unsaved project',
            [System.StringComparison]::Ordinal
        ) -ge 0 -and
        $MainWindowTitle.IndexOf(
            '*',
            [System.StringComparison]::Ordinal
        ) -lt 0 -and
        $MainWindowTitle.IndexOf(
            'read-only',
            [System.StringComparison]::OrdinalIgnoreCase
        ) -lt 0

    Add-Check `
        -Name 'window-launch' `
        -Passed $LaunchTitleValid `
        -Detail "Main window uses the established clean unsaved-project title contract: '$MainWindowTitle'."

    Set-Stage 'home-default'

    $HomeElement = Wait-ByName `
        -Root $Root `
        -Name 'MediaForge Home' `
        -ControlTypeName ''

    Add-Check `
        -Name 'home-default' `
        -Passed ($null -ne $HomeElement) `
        -Detail 'Task-first Home is present immediately after isolated launch.'

    Set-Stage 'home-control-tree'

    $Names = @(Get-ControlNames -Root $Root)
    $AdvancedLeak = @(
        $Names |
            Where-Object {
                $_ -eq 'Auto-detect' -or
                $_ -eq 'Download tools' -or
                $_ -eq 'FFmpeg tools'
            }
    )

    Add-Check `
        -Name 'advanced-absent-from-home' `
        -Passed ($AdvancedLeak.Count -eq 0) `
        -Detail 'FFmpeg/tool controls are absent from the default Home automation tree.'

    Set-Stage 'menu-keyboard-access'

    foreach ($MenuSpec in @(
        @{ Name = 'File'; Key = 'f' },
        @{ Name = 'Edit'; Key = 'e' },
        @{ Name = 'View'; Key = 'v' },
        @{ Name = 'Project'; Key = 'p' },
        @{ Name = 'Tools'; Key = 't' },
        @{ Name = 'Help'; Key = 'h' }
    )) {
        Assert-MenuKeyboardAccess `
            -Root $Root `
            -Target $Process `
            -Name $MenuSpec.Name `
            -AccessKey $MenuSpec.Key
    }

    Add-Check `
        -Name 'menu-keyboard-access' `
        -Passed $true `
        -Detail 'File/Edit/View/Project/Tools/Help each expanded through its Alt access key.'

    Set-Stage 'home-control-metadata'

    $AddMedia = Find-ByName `
        -Root $Root `
        -Name 'Add Media' `
        -ControlTypeName 'ControlType.Button'

    if ($null -eq $AddMedia) {
        throw 'Home Add Media button was not found.'
    }

    if (-not $AddMedia.Current.IsKeyboardFocusable) {
        throw 'Home Add Media button is not keyboard focusable.'
    }

    if ([string]::IsNullOrWhiteSpace([string]$AddMedia.Current.HelpText)) {
        throw 'Home Add Media button has no HelpText.'
    }

    $Resize = Find-ByName `
        -Root $Root `
        -Name 'Resize' `
        -ControlTypeName 'ControlType.Button'

    if ($null -eq $Resize) {
        throw 'Home Resize button was not found.'
    }

    if ([string]::IsNullOrWhiteSpace([string]$Resize.Current.HelpText)) {
        throw 'Home Resize button has no HelpText.'
    }

    Set-Stage 'home-tab-order'

    Focus-App -Target $Process
    $AddMedia.SetFocus()
    Start-Sleep -Milliseconds 100

    foreach ($Expected in @('Add Folder', 'Convert', 'Resize')) {
        [System.Windows.Forms.SendKeys]::SendWait('{TAB}')
        Start-Sleep -Milliseconds 150

        $Focused = [System.Windows.Automation.AutomationElement]::FocusedElement
        $FocusedName = if ($null -eq $Focused) {
            ''
        }
        else {
            [string]$Focused.Current.Name
        }

        if ($FocusedName -ne $Expected) {
            throw "Tab order mismatch. Expected '$Expected'; focused '$FocusedName'."
        }
    }

    Add-Check `
        -Name 'home-tab-order' `
        -Passed $true `
        -Detail 'Add Media -> Add Folder -> Convert -> Resize is keyboard reachable in logical order.'

    Set-Stage 'resize-keyboard-route'

    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    Start-Sleep -Milliseconds 300

    $BackToHome = Wait-ByName `
        -Root $Root `
        -Name 'Back to Home' `
        -ControlTypeName 'ControlType.Button'

    $TaskAddMedia = Wait-ByName `
        -Root $Root `
        -Name 'Add Media' `
        -ControlTypeName 'ControlType.Button'

    $TaskAddMediaFocusable =
        $null -ne $TaskAddMedia -and
        $TaskAddMedia.Current.IsKeyboardFocusable

    $ResizeRoutePassed =
        $null -ne $BackToHome -and
        $TaskAddMediaFocusable

    Add-Check `
        -Name 'resize-keyboard-route' `
        -Passed $ResizeRoutePassed `
        -Detail 'Enter on keyboard-focused Resize exposed the task-only Back to Home control and a keyboard-focusable task Add Media control.'

    Set-Stage 'advanced-collapsed'

    $Advanced = Wait-ByName `
        -Root $Root `
        -Name 'Advanced controls' `
        -ControlTypeName ''

    if ($null -eq $Advanced) {
        throw 'Advanced controls expander was not found in the task workspace.'
    }

    $AdvancedState = Get-ExpandState -Element $Advanced

    Add-Check `
        -Name 'advanced-collapsed' `
        -Passed ($AdvancedState -eq [System.Windows.Automation.ExpandCollapseState]::Collapsed) `
        -Detail 'Advanced controls are collapsed on task entry.'

    if ([string]::IsNullOrWhiteSpace([string]$Advanced.Current.HelpText)) {
        throw 'Advanced controls expander has no HelpText.'
    }

    Set-Stage 'advanced-keyboard-route'

    Send-Key -Target $Process -Keys '%t'
    [System.Windows.Forms.SendKeys]::SendWait('a')
    Start-Sleep -Milliseconds 350

    $AutoDetect = Wait-ByName `
        -Root $Root `
        -Name 'Auto-detect' `
        -ControlTypeName 'ControlType.Button'

    Add-Check `
        -Name 'advanced-keyboard-route' `
        -Passed ($null -ne $AutoDetect) `
        -Detail 'Tools -> Advanced workspace is keyboard reachable and exposes technical controls.'

    Set-Stage 'home-keyboard-return'

    Send-Key -Target $Process -Keys '%v'
    [System.Windows.Forms.SendKeys]::SendWait('h')
    Start-Sleep -Milliseconds 350

    $HomeAgain = Wait-ByName `
        -Root $Root `
        -Name 'MediaForge Home' `
        -ControlTypeName ''

    Add-Check `
        -Name 'home-keyboard-return' `
        -Passed ($null -ne $HomeAgain) `
        -Detail 'View -> Home returns to the task-first Home through keyboard access keys.'

    $NamesAfterReturn = @(Get-ControlNames -Root $Root)
    $AdvancedLeakAfterReturn = @(
        $NamesAfterReturn |
            Where-Object {
                $_ -eq 'Auto-detect' -or
                $_ -eq 'Download tools' -or
                $_ -eq 'FFmpeg tools'
            }
    )

    Add-Check `
        -Name 'advanced-hidden-after-return' `
        -Passed ($AdvancedLeakAfterReturn.Count -eq 0) `
        -Detail 'Advanced technical controls are absent from the Home automation tree after keyboard return.'

    Set-Stage 'workflow-status-automation'

    $WorkflowStatus = Find-ByName `
        -Root $Root `
        -Name 'Workflow status' `
        -ControlTypeName ''

    Add-Check `
        -Name 'workflow-status-automation-name' `
        -Passed ($null -ne $WorkflowStatus) `
        -Detail 'Workflow status exposes an automation name on the visible shell.'

    # VAL-082 ends after the shell/accessibility assertions above.
    # Graceful application shutdown is covered separately by launch smoke.
    # The isolated validator-owned process is terminated in finally so an
    # unsaved-project confirmation cannot turn production safety behaviour
    # into a false PH-22 shell-validation failure.
    Set-Stage 'complete'
}
catch {
    $Failure = $_
    Write-Host "VAL-082 FAILURE STAGE: $script:Stage"
    Write-Host "VAL-082 FAILURE TYPE: $($_.Exception.GetType().FullName)"
    Write-Host "VAL-082 FAILURE MESSAGE: $($_.Exception.Message)"

    if (-not [string]::IsNullOrWhiteSpace([string]$_.ScriptStackTrace)) {
        Write-Host "VAL-082 FAILURE STACK: $($_.ScriptStackTrace)"
    }
}
finally {
    if ($null -ne $Process -and -not $Process.HasExited) {
        Stop-ProcessTree -Target $Process
    }

    $FailureType = $null
    $FailureMessage = $null
    $FailureStack = $null

    if ($null -ne $Failure) {
        $FailureType = $Failure.Exception.GetType().FullName
        $FailureMessage = $Failure.Exception.Message
        $FailureStack = $Failure.ScriptStackTrace
    }

    $Document = [ordered]@{
        schema = 2
        validation = 'VAL-082'
        stage = $script:Stage
        executable = $ResolvedExecutable
        executableSha256 = if (Test-Path -LiteralPath $ResolvedExecutable) {
            (Get-FileHash -LiteralPath $ResolvedExecutable -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        else {
            $null
        }
        projectRoot = $ResolvedProjectRoot
        generatedUtc = [DateTime]::UtcNow.ToString('o')
        startedUtc = $StartedUtc.ToString('o')
        completedUtc = [DateTime]::UtcNow.ToString('o')
        osVersion = [Environment]::OSVersion.VersionString
        powershell = $PSVersionTable.PSVersion.ToString()
        highContrastAtRun = $HighContrastAtRun
        primaryScreen = [ordered]@{
            x = $ScreenBounds.X
            y = $ScreenBounds.Y
            width = $ScreenBounds.Width
            height = $ScreenBounds.Height
        }
        mainWindowHandle = $MainWindowHandle
        mainWindowTitle = $MainWindowTitle
        passed = ($null -eq $Failure)
        checks = $script:Checks
        limitation = 'VAL-082 proves the PH-22 shell baseline only. It does not prove end-to-end Resize processing, screen-reader usability, contrast-theme visual quality at every theme, or DPI/multi-monitor layout across declared scales.'
        failure = if ($null -eq $Failure) {
            $null
        }
        else {
            [ordered]@{
                type = $FailureType
                message = $FailureMessage
                stack = $FailureStack
            }
        }
    }

    $EvidenceDirectory = Split-Path -Parent $EvidencePath
    if ($EvidenceDirectory) {
        New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
    }

    $Document |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $EvidencePath -Encoding UTF8

    Remove-Item -LiteralPath $IsolationRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "PH-22 shell evidence: $EvidencePath"

if ($null -ne $Failure) {
    throw $Failure
}

Write-Host 'VAL-082 PASSED: isolated Home/menu/task/Advanced keyboard shell baseline.'
