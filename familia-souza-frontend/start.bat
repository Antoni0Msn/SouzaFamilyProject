@echo off
cd /d "%~dp0"
where py >nul 2>&1
if %errorlevel%==0 (
  py -m http.server 5500
  goto :eof
)
where python >nul 2>&1
if %errorlevel%==0 (
  python -m http.server 5500
  goto :eof
)
echo Python nao encontrado.
echo Alternativa: abra a pasta no VS Code e use Live Server.
pause
