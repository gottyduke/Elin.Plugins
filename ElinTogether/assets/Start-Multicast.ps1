$steam = "$($env:SteamPath)\steam.exe"

& $steam "steam://rungameid/2135150"

$startExe = Join-Path $env:SandboxPath "Start.exe"

& $startExe /box:D1 $steam "steam://rungameid/2135150"

if ($args[0]) {
    & $startExe /box:D2 $steam "steam://rungameid/2135150"
}