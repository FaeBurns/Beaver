@echo off
setlocal enabledelayedexpansion

REM === Configuration ===
set "PROJECT_ROOT=%~dp0"
set "PLUGIN_SRC=%PROJECT_ROOT%NativePlugin"
set "WINDOWS_BUILD=%PROJECT_ROOT%build_win"
set "ANDROID_BUILD=%PROJECT_ROOT%build_android"
set "NDK_PATH=C:/Program Files/Unity/Hub/Editor/6000.0.21f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK"  REM <-- Adjust as needed
set "ANDROID_API=24"

REM === Windows Build ===
echo.
echo === Building Windows Plugin ===
if not exist "%WINDOWS_BUILD%" mkdir "%WINDOWS_BUILD%"
pushd "%WINDOWS_BUILD%"
cmake -S "%PLUGIN_SRC%" -B "%WINDOWS_BUILD%" -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
popd

REM Copy DLL
set "DLL_PATH=%WINDOWS_BUILD%\Release\gpu_timer.dll"
set "DEST_WIN=%PROJECT_ROOT%Assets\Plugins\x86_64"
if not exist "%DEST_WIN%" mkdir "%DEST_WIN%"
copy /Y "%DLL_PATH%" "%DEST_WIN%"

REM === Android Build ===
echo.
echo === Building Android Plugin ===
if not exist "%ANDROID_BUILD%" mkdir "%ANDROID_BUILD%"
pushd "%ANDROID_BUILD%"
cmake -S "%PLUGIN_SRC%" -B "%ANDROID_BUILD%" ^
  -G "Ninja" ^
  -DCMAKE_TOOLCHAIN_FILE="C:/Program Files/Unity/Hub/Editor/6000.0.21f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK\build\cmake\android.toolchain.cmake" ^
  -DANDROID_ABI=%ANDROID_ABI% ^
  -DANDROID_PLATFORM=android-%ANDROID_API% ^
  -DCMAKE_BUILD_TYPE=Release

cmake --build . --config Release
popd

REM Copy SO
set "SO_PATH=%ANDROID_BUILD%\libgpu_timer.so"
if not exist "%SO_PATH%" (
  REM Sometimes it's in a subdir like build_android\CMakeFiles\gpu_timer.dir\Release
  for /R "%ANDROID_BUILD%" %%f in (libgpu_timer.so) do (
    set "SO_PATH=%%f"
    goto :found_so
  )
)

:found_so
set "DEST_ANDROID=%PROJECT_ROOT%Assets\Plugins\Android"
if not exist "%DEST_ANDROID%" mkdir "%DEST_ANDROID%"
copy /Y "!SO_PATH!" "%DEST_ANDROID%"

echo.
echo === Build Complete ===
pause
