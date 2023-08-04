@echo off
cd KDMapper
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
cd ..

cd Somno
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
cd ..

cd Somno.ILTransformer
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
cd ..

cd Somno.Packager
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
cd ..

cd Somno.Portal
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
cd ..

cd Somno.WindowHost
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
cd ..

echo [+] clean-up finished