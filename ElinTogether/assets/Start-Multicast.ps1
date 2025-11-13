

$instance1 = "$($env:ElinGamePath)\Elin.exe"
& $instance1

$instance2 = "$($env:SandboxPath)\Start.exe"
& $instance2 /box:D1 $instance1

if ($args[0] -eq $True) {
    & $instance2 /box:D2 $instance1
}