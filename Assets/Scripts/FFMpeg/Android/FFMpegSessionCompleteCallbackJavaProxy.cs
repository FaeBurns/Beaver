using System;
using UnityEngine;

namespace FFMpeg.Android
{
    public class FFMpegSessionCompleteCallbackJavaProxy : AndroidJavaProxy
    {
        public FFMpegSessionCompleteCallbackJavaProxy() : base("com.arthenica.ffmpegkit.FFmpegSessionCompleteCallback")
        {
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Global
        public void apply(AndroidJavaObject session)
        {
            Debug.Log($"FFMpeg Session completed");
        }
    }
}