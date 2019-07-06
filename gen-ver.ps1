$majorVersion = 4
$minorVersion = 0
$version = "$majorVersion.$minorVersion"
$fileVersion = "$majorVersion.$minorVersion" + "." + (Get-Date -Format yy) + (Get-Date).DayOfYear + "." + (Get-Date -Format HHmm)

Get-ChildItem -Path $PSScriptRoot -Filter "*.csproj" -Recurse | ForEach-Object {
    [xml]$doc = Get-Content $_.FullName
    if ($doc.Project.Sdk -eq "Microsoft.NET.Sdk") {
        if ($doc.Project.PropertyGroup.Version) {
            $doc.Project.PropertyGroup.Version = $fileVersion
        }
        if ($doc.Project.PropertyGroup.FileVersion) {
            $doc.Project.PropertyGroup.FileVersion = $fileVersion
        }
        if ($doc.Project.PropertyGroup.AssemblyVersion) {
            $doc.Project.PropertyGroup.AssemblyVersion = $version
        }
        $doc.Save($_.FullName)
    }
}

Write-Host $fileVersion