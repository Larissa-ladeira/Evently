@echo off
rem Arranca o backend (requer build previo: dotnet build Evently.Api)
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:5072
cd /d "%~dp0Evently.Api\bin\Debug\net10.0"
"Evently.Api.exe"
