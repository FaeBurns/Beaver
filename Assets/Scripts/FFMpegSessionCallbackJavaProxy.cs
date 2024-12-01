using UnityEngine;

public class FFMpegSessionCallbackJavaProxy : AndroidJavaProxy
{
    public FFMpegSessionCallbackJavaProxy() : base("com.arthenica.ffmpegkit.FFmpegSessionCompleteCallback")
    {
    }

    public override AndroidJavaObject Invoke(string methodName, AndroidJavaObject[] javaArgs)
    {
        // AndroidJavaObject session = javaArgs[0];
        Debug.Log($"invoked callback {methodName}");

        // Debug.Log("Session ended");

        return null;
    }
}