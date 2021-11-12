
param (
    [string] $addrFile
)

if(($addrFile)) 
{
    $excelPath = [System.IO.Path]::GetDirectoryName($addrFile)
    .\check-address.exe -x $addrFile
    .\gen-routes -i  $excelPath"\GoodAddresses.csv"
    .\gen-report -d $excelPath"\Deliveries.txt"
}
else { 
    Write-Host "Error usage: do-deliveries.ps1 -addrFile P:\PathTo\ExcelFile.xlsx" 
}
