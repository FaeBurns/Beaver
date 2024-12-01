using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public static class FFMpegKitInterop
{
    private static bool s_initialized = false;

    public static void Init()
    {
        if (s_initialized)
            throw new InvalidOperationException($"{nameof(FFMpegKitInterop)} has already been initialized.");

        // disable SIGXCPU signal
        // required to stop timeouts on long operations
        AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig");
        AndroidJavaObject paramVal = new AndroidJavaClass("com.arthenica.ffmpegkit.Signal").GetStatic<AndroidJavaObject>("SIGXCPU");
        configClass.CallStatic("ignoreSignal", new object[] { paramVal } );

        s_initialized = true;
    }

    public static void StreamToFile(IPEndPoint server, string filePath)
    {
        // 127.0.0.1:9943
        // ExecuteAsync($"-f mpegts -i tcp://{server.Address}:{server.Port}\\?listen \"{filePath}\"");
        // ExecuteAsync($"-f flv -i rtmp://127.0.0.1:9943/rtmp_stream/mystream \"{filePath}\"");
        // ExecuteAsync($"-f mpegts -probesize 32 -fflags nobuffer -flags low_delay -framedrop -sync ext -i tcp://127.0.0.1:10755 \"{filePath}\"");
        // ExecuteAsync($"-f mpegts -probesize 32 -fflags nobuffer -flags low_delay -t 60 -i tcp://127.0.0.1:10757 \"{filePath}\"");
        ExecuteAsync(String.Format(File.ReadAllText(Application.persistentDataPath + "/command.txt"), filePath));
    }

    private static void ExecuteAsync(string args)
    {
        AndroidJavaClass ffmpegKitClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKit");

        // null in place of callback for now
        AndroidJavaObject session = ffmpegKitClass.CallStatic<AndroidJavaObject>("executeAsync", args, new FFMpegSessionCallbackJavaProxy());

        Thread.Sleep(15 * 1000);
        Debug.Log(session.Call<String>("getOutput"));
    }

    private static void Execute(string args)
    {
        AndroidJavaClass ffmpegKitClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKit");

        // null in place of callback for now
        ffmpegKitClass.CallStatic<AndroidJavaObject>("execute", args);
    }
}