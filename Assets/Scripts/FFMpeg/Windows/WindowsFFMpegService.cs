using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Threading;
using UnityEngine;

namespace FFMpeg.Windows
{
    public class WindowsFFMpegService : IFFMpegService
    {
        public void Execute(string args)
        {
            WindowsFFMpegExec exec = new WindowsFFMpegExec(args, true);
        }

        public void ExecuteAsync(string args)
        {
            WindowsFFMpegExec exec = new WindowsFFMpegExec(args);
        }

        public Stream OpenStreamPipe(IPEndPoint server, int width, int height, int frameRate)
        {
            string pipeName = "BeaverFromFFmpegPipe";
            NamedPipeServerStream inputPipe = new NamedPipeServerStream(pipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.WriteThrough, 1024, 1024); // TODO: check buffer sizes

            string pipeString = @"\\.\pipe\" + pipeName;
            string formatArgs = "-f mpegts";
            string videoArgs = $"-framerate {frameRate} -video_size {width}x{height}";
            string args = $"{formatArgs} -probesize 32 -fflags nobuffer -flags low_delay -i tcp://{server.Address}:{server.Port} -f rawvideo {videoArgs} {pipeString}";
            // string args = $"{formatArgs} -probesize 32 -fflags nobuffer -flags low_delay -i tcp://{server.Address}:{server.Port} -f rawvideo {videoArgs} pipe:1";

            Debug.Log($"executing ffmpeg with args {args}");
            // WindowsFFMpegExec exec = new WindowsFFMpegExec(args);
            Debug.Log("Waiting for pipe connection...");
            inputPipe.WaitForConnection();
            Debug.Log("Pipe connection established.");

            return inputPipe;
        }
    }
}