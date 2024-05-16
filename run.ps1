##### <Config>
$gameName = "The Farmer Was Replaced"
$gameRoot = "C:\Program Files (x86)\Steam\steamapps\common\$gameName"
$pluginsRoot = "${gameRoot}\BepInEx\plugins"
##### </Config>

#### <Init>
$ErrorActionPreference = "Stop"
$projectName = "EnhancedPython"
$processName = "TheFarmerWasReplaced"
#### </Init>

##### <BeforeBuild>
$shouldRelaunch=0
$process = Get-Process -Name $processName -ErrorAction SilentlyContinue
if ($process) {
    $shouldRelaunch=1
    Stop-Process -Id $process.Id -ErrorAction Stop
    $process.WaitForExit()
}
##### </BeforeBuild>

##### <Utils>
function New-TempDir {
    ## Source: https://stackoverflow.com/a/54935264
    
    $parent = [System.IO.Path]::GetTempPath()
    do {
        $name = [System.IO.Path]::GetRandomFileName()
        $item = New-Item -Path $parent -Name $name -ItemType "directory" -ErrorAction SilentlyContinue
    } while (-not $item)
    return $item.FullName
}
##### </Utils>

##### <Build>
$buildDir = New-TempDir

dotnet build -c=Release -o="$buildDir" --nologo "$projectName.csproj"
if (-not $?) {
    Write-Host "Failed to build project"
    return 1
}

$assemblyPath = "$buildDir\$projectName.dll"
$pdbPath = "$buildDir\$projectName.pdb"

Copy-Item -Path $assemblyPath -Destination $pluginsRoot -Force
Copy-Item -Path $pdbPath -Destination $pluginsRoot -Force
##### </Build>

##### <AfterBuild>
Remove-Item -Path $buildDir -Recurse -Force

if ($shouldRelaunch -eq 1) {
    start steam://rungameid/2060160
}
##### </AfterBuild>
