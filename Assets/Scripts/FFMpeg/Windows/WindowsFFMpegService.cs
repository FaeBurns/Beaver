using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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

        public async Task<Stream> OpenStreamServer(IPEndPoint server, int width, int height, int frameRate)
        {
            // return OpenStreamServerSocket(server, width, height, frameRate);
            return await OpenStreamServerPipe(server, width, height, frameRate);
        }

        private Stream OpenStreamServerSocket(IPEndPoint server, int width, int height, int frameRate)
        {
            // port here is for the incoming stream from ffmpeg, not the stream ffmpeg is receiving
            // server is the address that ffmpeg has to listen on
            TcpListener listener = new TcpListener(IPAddress.Loopback, 9944);
            listener.Start();
            Debug.Log("stream listener started");
            TcpClient client = listener.AcceptTcpClient();
            Debug.Log("client connected");

            return client.GetStream();
        }

        private async Task<Stream> OpenStreamServerPipe(IPEndPoint server, int width, int height, int frameRate)
        {
            int bufferMult = 5;
            int bufferSize = (width * height * 4) * bufferMult;

            Debug.Log($"Allocating pipe with buffer size of {bufferSize} | mult: {bufferMult}, based on size of {width * height * 4}");

            string pipeName = "BeaverFromFFmpegPipe";
            NamedPipeServerStream inputPipe = new NamedPipeServerStream(pipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.WriteThrough, bufferSize, bufferSize); // TODO: check buffer sizes

            string pipeString = @"\\.\pipe\" + pipeName;
            string protocol = "tcp";

            string argsInput  = $"-probesize 8192 -fflags nobuffer -flags low_delay -i {protocol}://{server.Address}:{server.Port}";
            string argsOutput = $"-f rawvideo -pix_fmt argb -colorspace bt709 -vcodec rawvideo -r {frameRate} -video_size {width}x{height} {pipeString} -y";
            string args = argsInput + " " + argsOutput;

            Debug.Log($"executing ffmpeg with args {args}");
            WindowsFFMpegExec exec = new WindowsFFMpegExec(args);
            Debug.Log("Waiting for pipe connection...");
            await inputPipe.WaitForConnectionAsync(Application.exitCancellationToken);
            Debug.Log("Pipe connection established.");

            return inputPipe;
        }
    }
}