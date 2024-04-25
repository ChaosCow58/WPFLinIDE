param (
    [Parameter(Mandatory=$true)] [string]$new_title
)

$Host.UI.RawUI.WindowTitle = $new_title

Function Start-ExitMode {
    Write-Host "Press any key to exit..."
    $key = $null
    while ($null -eq $key) {
        $key = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown").Character
        Stop-Process -Id $PID
    }
}

Start-ExitMode
