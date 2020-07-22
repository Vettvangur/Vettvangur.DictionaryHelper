nuget restore .
Remove-Item *.nupkg
Remove-Item *.snupkg
nuget pack -build -Symbols -SymbolPackageFormat snupkg -Properties Configuration=Release
$pkg = gci *.nupkg 
nuget push $pkg -Source https://www.nuget.org/api/v2/package -NonInteractive -SkipDuplicate
$snupkg = gci *.snupkg 
Write-Host Pushing symbols to https://www.nuget.org/api/v2/package
nuget push $snupkg -Source https://www.nuget.org/api/v2/package -NonInteractive -verb
