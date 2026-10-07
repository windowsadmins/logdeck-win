<#
.SYNOPSIS
    Pre-installation script for the LogDeck package.

.DESCRIPTION
    Closes LogDeck if anyone has it open: it runs from the install folder, whose files are
    about to be replaced. It holds no state, so closing it loses nothing. Never fails the
    install.
#>

$running = Get-Process -Name 'LogDeck' -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "[LogDeck] Closing LogDeck ($($running.Count) window(s))"
    $running | Stop-Process -Force -ErrorAction SilentlyContinue
}

exit 0
