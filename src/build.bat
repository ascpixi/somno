@echo off
call env.bat

cd Somno.ILTransformer
dotnet publish -r win-x64 -c %1
if %errorlevel% neq 0 goto fail
cd ..

cd Somno.Packager
dotnet publish -r win-x64 -c %1 -p:IsCLI=true
if %errorlevel% neq 0 goto fail
cd ..

cd Somno.Portal
"%MSBUILD_PATH%" Somno.Portal.vcxproj /p:Configuration=%1 /p:Platform=x64
if %errorlevel% neq 0 goto fail
cd ..

cd Somno.WindowHost
"%MSBUILD_PATH%" Somno.WindowHost.vcxproj /p:Configuration=%1 /p:Platform=x64
if %errorlevel% neq 0 goto fail
cd ..

cd Somno
dotnet publish -r win-x64 -c %1 /p:RepackagePortalAgent=true /p:RepackageWindowHost=true
if %errorlevel% neq 0 goto fail
cd ..

echo.
echo.
echo Build done!
goto :EOF

:fail
    cd ..
    echo.
    echo.
    echo Build failed.