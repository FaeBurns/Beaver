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
            int bufferMult = 20;
            int bufferSize = (width * height * 4) * bufferMult;

            Debug.Log($"Allocating pipe with buffer size of {bufferSize} | mult: {bufferMult}, based on size of {width * height * 4}");

            string pipeName = "BeaverFromFFmpegPipe";
            NamedPipeServerStream inputPipe = new NamedPipeServerStream(pipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.WriteThrough, bufferSize, bufferSize); // TODO: check buffer sizes

            string pipeString = @"\\.\pipe\" + pipeName;
            string incomingFormat = "mpegts";
            string protocol = "udp";
            string outputFormat = $"-f rawvideo -pixel_format rgba -colorspace bt709 -vcodec rawvideo -r {frameRate} -video_size {width}x{height}";
            string args = $"-f {incomingFormat} -probesize 8192 -fflags nobuffer -flags low_delay -i {protocol}://{server.Address}:{server.Port} {outputFormat} {pipeString} -y";
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