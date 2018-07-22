$dirIn = "C:\work\DenebLab\LibZ\out\tool"
$dirOut = "C:\work\DenebLab\Syrup\src\build\tools\LibZ.Tool\tools"

Copy-Item "$dirIn\*" -destination $dirOut -recurse -Force
