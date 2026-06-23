@echo off
setlocal EnableExtensions EnableDelayedExpansion

echo Cleaning build artifacts under:
echo %CD%
echo.

REM --- Delete bin, obj, and .vs folders ---
for /d /r %%D in (bin obj .vs) do (
    if exist "%%D" (
        echo Deleting folder: %%D
        rd /s /q "%%D"
    )
)

REM --- Delete *.vsidx files ---
for /r %%F in (*.vsidx) do (
    if exist "%%F" (
        echo Deleting file: %%F
        del /f /q "%%F"
    )
)

echo.
echo Cleanup complete.
pause
