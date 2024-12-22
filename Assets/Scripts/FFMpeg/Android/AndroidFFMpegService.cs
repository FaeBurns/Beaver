using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FFMpeg.Android
{
    public class AndroidFFMpegService : IFFMpegService
    {
        private static readonly AndroidJavaClass s_ffmpegKitClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKit");
        private static readonly AndroidJavaClass s_ffmpegKitConfigClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig");

        static AndroidFFMpegService()
        {
            // disable SIGXCPU signal
            // required to stop timeouts on long operations
            AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig");
            AndroidJavaObject paramVal = new AndroidJavaClass("com.arthenica.ffmpegkit.Signal").GetStatic<AndroidJavaObject>("SIGXCPU");
            configClass.CallStatic("ignoreSignal", paramVal);
        }

        public void Execute(string args)
        {
            Debug.Log($"Executing Synchronous FFMpeg with args: {args}");

            // null in place of callback for now
            AndroidJavaObject session = s_ffmpegKitClass.CallStatic<AndroidJavaObject>("execute", args, new FFMpegSessionCompleteCallbackJavaProxy(), new FFMpegLogCallbackJavaProxy(), null);
            session.Dispose();
        }

        public void ExecuteAsync(string args)
        {
            Debug.Log($"Executing Asynchronous FFMpeg with args: {args}");

            // this may leak session AJOs but it's not gonna be called often so it's probably fine
            s_ffmpegKitClass.CallStatic<AndroidJavaObject>("executeAsync", args, new FFMpegSessionCompleteCallbackJavaProxy(), new FFMpegLogCallbackJavaProxy(), null);
        }

        public async Task<Stream> OpenStreamServer(IPEndPoint server, int width, int height, int frameRate)
        {
            using AndroidJavaObject context = GetCurrentActivity();

            // create pipe
            // string pipeString = s_ffmpegKitConfigClass.CallStatic<string>("registerNewFFmpegPipe", context);

            int pipePort = 9945;
            string pipeString = $@"tcp://127.0.0.1:{pipePort}\?listen";
            string protocol = "tcp";

            string argsInput  = $"-f mpegts -probesize 8192 -fflags nobuffer -flags low_delay -i {protocol}://{server.Address}:{server.Port}";
            string argsOutput = $"-f rawvideo -pix_fmt argb -colorspace bt709 -vcodec rawvideo -r {frameRate} -video_size {width}x{height} {pipeString} -y";
            string args = argsInput + " " + argsOutput;

            Debug.Log($"Running ffmpeg with args {args}");

            s_ffmpegKitClass.CallStatic<AndroidJavaObject>("executeAsync", args, new FFMpegSessionCompleteCallbackJavaProxy(), new FFMpegLogCallbackJavaProxy(), null);

            // sleep to allow tcp server to start
            await Task.Delay(TimeSpan.FromSeconds(5));

            Debug.Log("Waiting for for listener");
            TcpClient client = new TcpClient();
            client.Connect(new IPEndPoint(IPAddress.Loopback, pipePort));
            Debug.Log("Client accepted");
            return client.GetStream();

            // Stream stream = File.OpenRead(pipeString);
            // return stream;
            // return inputPipe;
        }

        private static AndroidJavaObject GetCurrentActivity()
        {
            using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }
    }
}