<#
.SYNOPSIS
    Uninstall script for the LogDeck package.

.DESCRIPTION
    Closes LogDeck and removes the all-users Start menu shortcut postinstall.ps1 created,
    the one thing the package adds outside its install folder. Never fails the uninstall.
#>

$shortcutPath = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\LogDeck.lnk'

Get-Process -Name 'LogDeck' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

try {
    if (Test-Path -LiteralPath $shortcutPath) {
        Remove-Item -LiteralPath $shortcutPath -Force
        Write-Host "[LogDeck] Removed Start menu shortcut: $shortcutPath"
    }
} catch {
    Write-Host "[LogDeck] WARNING: could not remove the Start menu shortcut: $($_.Exception.Message)"
}

exit 0
