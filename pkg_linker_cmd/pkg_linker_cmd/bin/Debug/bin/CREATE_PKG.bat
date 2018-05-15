:: ------------------------------------------------------------------
:: Simple script to build a proper PKG using Python (by CaptainCPS-X)
:: ------------------------------------------------------------------
@echo off
cd %~dp0

:: Change this depending where you installed Python...
set PYTHON=c:\Python27

:: Don't change these...
set PATH=%PYTHON%;%PATH%
set PKG=.\pkg.py

:: Change these for your application / manual...
set CONTENTID=UP0100-PKGLINKER_00-PINK1000DEVIL303
set PKG_DIR=./PS3_GAME/
set PKG_NAME=./%CONTENTID%.pkg

:: This will run the Python PKG script...
python.exe %PKG% --contentid %CONTENTID% %PKG_DIR% %PKG_NAME%
