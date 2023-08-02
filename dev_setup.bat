@echo off

echo [ ] downloading tools...
call download_tools.bat

echo [ ] environment variables needed
set /p vInputMsbPath=[?] MSBuild path: 

echo :: Set all build environment variables here. > .\src\env.bat
echo set MSBUILD_PATH=%vInputMsbPath% >> .\src\env.bat

echo [+] done!