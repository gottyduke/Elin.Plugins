

$instance1 = "$($env:ElinGamePath)\Elin.exe"
& $instance1

$instance2 = "$($env:SandboxPath)\Start.exe"
& $instance2 /box:DefaultBox $instance1