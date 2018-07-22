$buildTestsDir = Split-Path -parent $PsScriptRoot
$inDir = Join-Path $PsScriptRoot "in"

$srcDir = Split-Path -parent $buildTestsDir
$rootDir = Split-Path -parent $srcDir
$outDir = Join-Path $rootDir "out"
$testDir = Join-Path $outDir "test"
$libz =  Join-Path $outDir "tool/libz.exe"


If (-not (Test-Path $testDir)){
        New-Item -ItemType directory -Path $testDir
}

Get-Childitem $testDir -File | Foreach-Object {Remove-Item $_.FullName}
Copy-Item "$inDir\*" -destination $testDir -recurse

Push-Location 
Set-Location $testDir
try {
	& $libz inject-dll -a PkApp.exe -b PkApp.exe.config -i *.dll --move 
}
catch {

}
finally {
	Pop-Location
}


