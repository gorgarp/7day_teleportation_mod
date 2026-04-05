$GameDir = Resolve-Path "..\.."
$ManagedDir = "$GameDir\7DaysToDie_Data\Managed"
$HarmonyDir = "$GameDir\Mods\0_TFP_Harmony"
$OutDll = "Teleporters.dll"

$Sources = @(Get-ChildItem -Recurse -Filter "*.cs" -Path "Scripts","Harmony" | ForEach-Object { $_.FullName })

$Refs = @(
    "$ManagedDir\mscorlib.dll",
    "$ManagedDir\netstandard.dll",
    "$ManagedDir\System.dll",
    "$ManagedDir\System.Core.dll",
    "$ManagedDir\System.Runtime.dll",
    "$ManagedDir\Assembly-CSharp.dll",
    "$ManagedDir\Assembly-CSharp-firstpass.dll",
    "$ManagedDir\LogLibrary.dll",
    "$ManagedDir\UnityEngine.dll",
    "$ManagedDir\UnityEngine.CoreModule.dll",
    "$ManagedDir\UnityEngine.PhysicsModule.dll",
    "$HarmonyDir\0Harmony.dll"
)

$RefArgs = ($Refs | ForEach-Object { "/reference:`"$_`"" }) -join " "
$SourceArgs = ($Sources | ForEach-Object { "`"$_`"" }) -join " "

$RoslynPath = Get-ChildItem -Recurse -Filter "csc.dll" -Path "$env:ProgramFiles\dotnet\sdk" |
    Where-Object { $_.FullName -like "*8.0*\Roslyn*" -or $_.FullName -like "*10.0*\Roslyn*" } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $RoslynPath) {
    Write-Error "Could not find Roslyn csc.dll in dotnet SDK"
    exit 1
}

Write-Host "Using compiler: $($RoslynPath.FullName)"
Write-Host "Compiling $($Sources.Count) source files..."

$cmd = "dotnet `"$($RoslynPath.FullName)`" /target:library /out:`"$OutDll`" /nostdlib /langversion:latest $RefArgs $SourceArgs"
Invoke-Expression $cmd

if (Test-Path $OutDll) {
    $size = (Get-Item $OutDll).Length
    Write-Host "Build succeeded: $OutDll ($size bytes)"
} else {
    Write-Error "Build failed - no DLL produced"
}
