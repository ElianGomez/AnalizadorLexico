@echo off
setlocal
echo === Compilando lexer.l con FLEX ===
win_flex -o lexer.c lexer.l
if errorlevel 1 (
  echo *** ERROR al generar lexer.c ***
  pause
  exit /b 1
)

echo === Compilando lexer.c con cl (Visual Studio) ===
cl /nologo /O2 lexer.c /Fe:lexer.exe
if errorlevel 1 (
  echo *** ERROR de compilacion ***
  pause
  exit /b 1
)

echo.
echo === OK: lexer.exe generado correctamente ===
pause
exit /b 0
