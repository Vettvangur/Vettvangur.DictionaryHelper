dotnet restore .

Remove-Item *.nupkg
Remove-Item *.snupkg

dotnet pack --include-symbols -c Release -o .

$pkg = gci *.nupkg 
nuget push $pkg -Source https://www.nuget.org/api/v2/package -NonInteractive -SkipDuplicate

Write-Host Pushing symbols to https://www.nuget.org/api/v2/package
$snupkg = gci *.snupkg 
nuget push $snupkg -Source https://www.nuget.org/api/v2/package -NonInteractive

pause
