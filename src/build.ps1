$sdks = "C:\Program Files (x86)\Microsoft SDKs\Windows"
$sns = Get-ChildItem -Path $sdks -Filter sn.exe -Recurse -ErrorAction SilentlyContinue -Force
$sn1 = $sns | Select-Object -ExpandProperty FullName
$sn = $sn1[0]
& $sn -k ../LibZ.snk
& ./fake.cmd