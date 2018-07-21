$srcPath = $PsScriptRoot
$rootPath = Split-Path -parent $srcPath
$keyFile = Join-Path $rootPath LibZ.snk
$nuget = Join-Path $srcPath "tools\NuGet\NuGet.exe"


If (-not (Test-Path $keyFile)){
    "Generate sn key: $keyFile"
    $sdks = "C:\Program Files (x86)\Microsoft SDKs\Windows"
    $sns = Get-ChildItem -Path $sdks -Filter sn.exe -Recurse -ErrorAction SilentlyContinue -Force
    $sn1 = $sns | Select-Object -ExpandProperty FullName
    $sn = $sn1[0]
    & $sn -k $keyFile
}

If (-not (Test-Path  $nuget)){
    "Download nuget: $nuget"
    $dir = [System.IO.Path]::GetDirectoryName($nuget)
    If (-not (Test-Path $dir)){
        New-Item -ItemType directory -Path $dir
    }
     Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nuget
}


& ./fake.cmd "Release"