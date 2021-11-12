@echo off

IF %1.==. GOTO No1
set file=%1
FOR %%i IN ("%file%") DO (
    REM ECHO filedrive=%%~di
    set filepath=%%~di%%~pi
    REM ECHO filename=%%~ni
    REM ECHO fileextension=%%~xi
)
echo %filepath%
.\check-address.exe -x %file%
.\gen-routes -i  %filepath%%\GoodAddresses.csv
.\gen-report -d %filepath%%\Deliveries.txt
GOTO End1

:No1
  ECHO Usage: do-deliveries.bat C:\path\to\ExcelFile.xlsx
GOTO End1

:End1

