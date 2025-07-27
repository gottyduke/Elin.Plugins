$target = "$($env:ElinGamePath)\Package\Mod_CustomWhateverLoader\CustomWhateverLoader.dll"
$source = "$($env:SteamContentPath)\2135150\3370512305\CustomWhateverLoader.dll"
$suppressionFile = [xml](Get-Content -Path "$($PSScriptRoot)\apicompat.suppressions.xml")
$tempFile = "$($PSScriptRoot)\temp.suppressions.xml"

foreach ($suppression in $suppressionFile.Suppressions.Suppression) {
    $suppression.Left = $target
    $suppression.Right = $source
}

$suppressionFile.Save($tempFile)

& apicompat -l $target -r $source --suppression-file $tempFile --respect-internals --permit-unnecessary-suppressions

Remove-Item -Path $tempFile -Force -ErrorAction SilentlyContinue