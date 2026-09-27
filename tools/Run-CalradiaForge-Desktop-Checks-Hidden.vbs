Option Explicit

' Launch the existing WPF test BATs without creating a visible cmd/PowerShell
' window. This is intended for runs started from Explorer while the user's
' Calradia Forge Desktop window should remain in the foreground.
Dim fso, shell, scriptFolder, repoRoot, artifactRoot, stamp
Dim renderReport, logPath, statusPath, testBatch
Dim comSpec, command, resultCode, output

Set fso = CreateObject("Scripting.FileSystemObject")
Set shell = CreateObject("WScript.Shell")
scriptFolder = fso.GetParentFolderName(WScript.ScriptFullName)
repoRoot = fso.GetParentFolderName(scriptFolder)
artifactRoot = fso.BuildPath(repoRoot, "artifacts\desktop-focus-safe")

If Not fso.FolderExists(fso.BuildPath(repoRoot, "artifacts")) Then
    fso.CreateFolder(fso.BuildPath(repoRoot, "artifacts"))
End If
If Not fso.FolderExists(artifactRoot) Then fso.CreateFolder(artifactRoot)

stamp = TimeStamp(Now)
renderReport = fso.BuildPath(artifactRoot, "wpf-render-" & stamp & ".json")
logPath = fso.BuildPath(artifactRoot, "desktop-checks-" & stamp & ".log")
statusPath = fso.BuildPath(artifactRoot, "desktop-checks-" & stamp & ".status.txt")
testBatch = fso.BuildPath(scriptFolder, "Run-CalradiaForge-Tests.bat")
comSpec = shell.ExpandEnvironmentStrings("%ComSpec%")

If Not fso.FileExists(testBatch) Then
    WriteStatus statusPath, 2, logPath, "The Desktop .bat test launcher is missing."
    WScript.Quit 2
End If

Set output = fso.CreateTextFile(logPath, True, False)
output.WriteLine "Focus-safe Desktop checks started: " & Now
output.WriteLine "Desktop MVVM and WPF render tests are invoked through the existing .bat launcher."
output.WriteLine "The opt-in UIA PowerShell smoke check is intentionally not included in this silent runner."
output.Close

' Shell.Run's window style 0 hides cmd.exe. The test commands and reports remain
' ordinary .bat/JSON artifacts; this launcher never sends input to a WPF window.
command = Quote(comSpec) & " /d /s /c " & Quote(Quote(testBatch) & _
    " --desktop-only --no-pause --render-output " & Quote(renderReport) & _
    " >> " & Quote(logPath) & " 2>&1")
resultCode = shell.Run(command, 0, True)

WriteStatus statusPath, resultCode, logPath, "Render report: " & renderReport
WScript.Quit resultCode

Function Quote(value)
    Quote = Chr(34) & value & Chr(34)
End Function

Function TimeStamp(value)
    TimeStamp = Year(value) & Pad2(Month(value)) & Pad2(Day(value)) & "-" & _
        Pad2(Hour(value)) & Pad2(Minute(value)) & Pad2(Second(value))
End Function

Function Pad2(value)
    Pad2 = Right("0" & CStr(value), 2)
End Function

Sub WriteStatus(path, exitCode, log, details)
    Dim file
    Set file = fso.CreateTextFile(path, True, True)
    file.WriteLine "ExitCode=" & exitCode
    file.WriteLine "Log=" & log
    file.WriteLine details
    file.Close
End Sub
