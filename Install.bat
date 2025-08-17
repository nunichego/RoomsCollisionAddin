@echo off
echo Installing Rooms Manager Add-in...

REM Create add-ins directory if it doesn't exist
if not exist "%APPDATA%\Autodesk\Revit\Addins\2024" mkdir "%APPDATA%\Autodesk\Revit\Addins\2024"

REM Copy DLL and add-in manifest
copy "bin\Debug\net48\RoomsManagerAddin.dll" "%APPDATA%\Autodesk\Revit\Addins\2024\"
copy "RoomsManagerAddin.addin" "%APPDATA%\Autodesk\Revit\Addins\2024\"

REM Copy dependencies
copy "bin\Debug\net48\*.dll" "%APPDATA%\Autodesk\Revit\Addins\2024\"

echo Installation complete!
echo Please restart Revit 2024 to load the add-in.
pause


