<#
.SYNOPSIS
    Post-installation script for the LogDeck package.

.DESCRIPTION
    Runs after the payload is copied to C:\Program Files\LogDeck. It gives LogDeck a Start
    menu entry for every user. LogDeck only reads logs, so there is no data folder, service
    or PATH entry to set up.

    cimipkg reads scripts/ out of the repository as checked out at the release tag, so this
    file is the one copy. It never fails the install: a missing shortcut is reported and the
    script still exits 0.
#>

$installDir = 'C:\Program Files\LogDeck'
$exe = Join-Path $installDir 'LogDeck.exe'
$shortcutPath = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\LogDeck.lnk'

# Start menu entry for every user. Save() overwrites, so a reinstall refreshes it.
if (Test-Path -LiteralPath $exe) {
    try {
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $exe
        $shortcut.WorkingDirectory = $installDir
        $shortcut.IconLocation = "$exe,0"
        $shortcut.Description = 'View the local logs of Windows management tools'
        $shortcut.Save()
        Write-Host "[LogDeck] Start menu shortcut: $shortcutPath"
    } catch {
        Write-Host "[LogDeck] WARNING: could not create the Start menu shortcut: $($_.Exception.Message)"
    }
} else {
    Write-Host "[LogDeck] LogDeck.exe not found at $exe; no Start menu shortcut"
}

exit 0
