using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FFMpeg.Windows
{
    public class WindowsFFMpegExec : IDisposable
    {
        public Process FfmpegProcess { get; }

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

            FfmpegProcess = ffmpegProcess;

            if (blocking)
            {
                ffmpegProcess.WaitForExit();
            }

            Task.Run(ApplicationExitTask);
            new Thread(ReadFFMpegOutput).Start();
        }

        private void ReadFFMpegOutput()
        {
            while (!Application.exitCancellationToken.IsCancellationRequested && !FfmpegProcess.HasExited)
            {
                Debug.Log(FfmpegProcess.StandardError.ReadToEnd());
            }
        }

        private async Task ApplicationExitTask()
        {
            while (!Application.exitCancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }

            if (FfmpegProcess != null && !FfmpegProcess.HasExited)
            {
                Debug.Log("Closing ffmpeg process");
                FfmpegProcess.Close();
            }
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}