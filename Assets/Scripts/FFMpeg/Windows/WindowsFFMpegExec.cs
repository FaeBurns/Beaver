using System;
using System.Diagnostics;
using System.IO;

using Debug = UnityEngine.Debug;

namespace FFMpeg.Windows
{
    public class WindowsFFMpegExec : IDisposable
    {
        public WindowsFFMpegExec(string arguments, bool blocking = false)
        {
            string exePath = Path.Join(UnityEngine.Application.streamingAssetsPath, "FFMpeg/Windows/ffmpeg.exe");

            Process ffmpegProcess = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            if (ffmpegProcess == null)
                throw new Exception("ffmpeg process not found");

            ffmpegProcess.EnableRaisingEvents = true;
            ffmpegProcess.Exited += (_, _) => Debug.Log($"FFMpeg process exited with code {ffmpegProcess.ExitCode}");

            ffmpegProcess.OutputDataReceived += (_, args) => Debug.Log("FFMpeg logged: " + args.Data);
            ffmpegProcess.ErrorDataReceived += (_, args) => Debug.LogError("FFMpeg errored: " + args.Data);

            if (blocking)
            {
                ffmpegProcess.WaitForExit();
            }
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}