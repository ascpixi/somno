@echo off
call env.bat

cd Somno.ILTransformer
dotnet publish -r win-x64 -c %1
cd ..

cd Somno.Packager
dotnet publish -r win-x64 -c %1 -p:IsCLI=true
cd ..

cd Somno.Portal
"%MSBUILD_PATH%" Somno.Portal.vcxproj /p:Configuration=%1 /p:Platform=x64
cd ..

cd Somno
dotnet publish -r win-x64 -c %1 /p:RepackagePortalAgent=true
cd ..

echo.
echo.
echo.
echo.
echo Build done!