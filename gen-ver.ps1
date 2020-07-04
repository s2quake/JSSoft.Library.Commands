$majorVersion = 5
$minorVersion = 0
$version = "$majorVersion.$minorVersion"
$fileVersion = "$majorVersion.$minorVersion" + "." + (Get-Date -Format yy) + (Get-Date).DayOfYear + "." + (Get-Date -Format HHmm)

Get-ChildItem -Path $PSScriptRoot -Include "*.csproj" -Depth 1 -Name | ForEach-Object {
    [xml]$doc = Get-Content $_ -Encoding UTF8
    if (($doc.Project.Sdk -eq "Microsoft.NET.Sdk") -or ($doc.Project.Sdk -eq "Microsoft.NET.Sdk.WindowsDesktop")) {
        foreach ($obj in $doc.Project.PropertyGroup) {        
            if ($obj.Version) {
                $obj.Version = $fileVersion
            }
            if ($obj.FileVersion) {
                $obj.FileVersion = $fileVersion
            }
            if ($obj.AssemblyVersion) {
                $obj.AssemblyVersion = $version
            }
        }
        $doc.Save($_)
    }
}

Set-Content version.txt $fileVersion -NoNewline -Encoding UTF8
Write-Host $fileVersion