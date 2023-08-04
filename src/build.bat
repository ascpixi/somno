@echo off
call env.bat

echo [ ] building Somno in %1 mode...

cd Somno.ILTransformer
dotnet publish -r win-x64 -c %1
if %errorlevel% neq 0 goto fail
cd ..

echo [+] built: Somno.ILTransformer

cd Somno.Packager
dotnet publish -r win-x64 -c %1 -p:IsCLI=true
if %errorlevel% neq 0 goto fail
cd ..

echo [+] built: Somno.Packager

cd Somno.WindowHost
dotnet publish -r win-x64 -c %1
if %errorlevel% neq 0 goto fail
cd ..

echo [+] built: Somno.WindowHost

cd Somno.Portal
"%MSBUILD_PATH%" Somno.Portal.vcxproj /p:Configuration=%1 /p:Platform=x64
if %errorlevel% neq 0 goto fail
cd ..

echo [+] built: Somno.Portal

cd KDMapper
"%MSBUILD_PATH%" KDMapper.vcxproj /p:Configuration=%1 /p:Platform=x64
if %errorlevel% neq 0 goto fail
cd ..

echo [+] built: KDMapper

cd Somno
dotnet publish -r win-x64 -c %1 /p:RepackagePortalAgent=true /p:RepackageWindowHost=true
if %errorlevel% neq 0 goto fail
cd ..

echo [+] built: Somno

echo.
echo.
echo [+] build done!
goto :EOF

:fail
    cd ..
    echo.
    echo.
    echo [-] build failed