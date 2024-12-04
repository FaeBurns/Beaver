using UnityEngine;

namespace FFMpeg.Android
{
    public class FFMpegLogCallbackJavaProxy : AndroidJavaProxy
    {
        public FFMpegLogCallbackJavaProxy() : base("com.arthenica.ffmpegkit.LogCallback")
        {
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Global
        public void apply(AndroidJavaObject log)
        {
            string logMessage = log.Call<string>("toString");
            Debug.Log($"FFMpegLog: {logMessage}");
        }
    }
}