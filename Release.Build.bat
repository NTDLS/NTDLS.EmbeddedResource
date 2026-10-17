@echo off
set path=%PATH%;C:\Program Files\7-Zip;

REM -------------------Build
rd .\Publish /q /s
md Publish

dotnet pack .\NTDLS.EmbeddedResource -c Release -o ".\Publish"

pause
