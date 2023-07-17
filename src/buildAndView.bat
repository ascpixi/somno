@echo off
call build.bat %1

explorer "%cd%\Somno\bin\%1\net7.0\win-x64\publish\"