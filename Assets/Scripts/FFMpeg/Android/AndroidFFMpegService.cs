using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FFMpeg.Android
{
    public class AndroidFFMpegService : IFFMpegService
    {
        private static readonly AndroidJavaClass s_ffmpegKitClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKit");

        static AndroidFFMpegService()
        {
            // disable SIGXCPU signal
            // required to stop timeouts on long operations
            AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig");
            AndroidJavaObject paramVal = new AndroidJavaClass("com.arthenica.ffmpegkit.Signal").GetStatic<AndroidJavaObject>("SIGXCPU");
            configClass.CallStatic("ignoreSignal", paramVal);

            File.Delete(Application.persistentDataPath + "/log.txt");
            Application.logMessageReceived += ApplicationOnlogMessageReceived;
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

        private static void ApplicationOnlogMessageReceived(string condition, string stacktrace, LogType type)
        {
            using StreamWriter sw = new StreamWriter(Application.persistentDataPath + "/log.txt", true);
            sw.WriteLine($"[{type}] | {condition}");
        }
    }
}