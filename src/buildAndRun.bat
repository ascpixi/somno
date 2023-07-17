@echo off
call build.bat %1

pushd %cd%
cd .\Somno\bin\%1\net7.0\win-x64\publish
Somno.exe
popd