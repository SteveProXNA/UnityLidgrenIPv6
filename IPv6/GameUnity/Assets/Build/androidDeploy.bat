@ECHO off
REM Android Deployment Script

REM Check Android Location. Anyone with a weird android location on their machine can add it here.
IF EXIST d:\android\ set androidlocation=d:\android\
IF EXIST c:\android\ set androidlocation=c:\android\

if not defined androidlocation (
	echo Android not found. Either Android SDK is not installed, or is in an unusual location.
	goto end
)

set versioncode=1
set apkname=com.steveproxna.gameunity


echo Android found at %androidlocation%

REM Set adb.exe location
set adb=%androidlocation%sdk\platform-tools\adb.exe
echo adb at %adb%


REM Horrendous batch file hack to read the android version into a variable.
for /f %%i in ('%adb% shell getprop ro.build.version.release') do set androidversion=%%i
echo Android version is %androidversion%

REM check for installation
for /f %%i in ('%adb% shell pm list packages %apkname%') do set installed=%%i
echo installed...%installed%

if (%installed%) == () goto skipcacheclear

REM clear cache
REM echo clearing cache...
REM %adb% shell pm clear %apkname%

:skipcacheclear

REM Uninstalling old version
REM echo Uninstalling old version if present...
REM %adb% uninstall %apkname%

REM Whatever the version, the apk install is the same.
echo Installing APK...
%adb% install -r %apkname%.apk



:end
echo "Press Any Key To Exit..."
pause > nul
