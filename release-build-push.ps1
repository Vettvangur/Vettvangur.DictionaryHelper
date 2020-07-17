nuget restore .
nuget pack -build -Symbols -SymbolPackageFormat snupkg -Properties Configuration=Release
$pkg = gci *.nupkg 
nuget push $pkg -Source https://www.nuget.org/api/v2/package -NonInteractive
$snupkg = gci *.snupkg 
nuget push $snupkg -Source https://www.nuget.org/api/v2/package -NonInteractive
