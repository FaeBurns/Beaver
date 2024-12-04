using System;
using System.IO;
using System.Net;
using UnityEngine;

namespace FFMpeg
{
    public class FFMpegInit : MonoBehaviour
    {
        private void Start()
        {
            if (File.Exists(Application.persistentDataPath + "/testout.mp4"))
                File.Delete(Application.persistentDataPath + "/testout.mp4");

            try
            {
                Debug.Log(Application.persistentDataPath + "/testout.mp4");
                StreamToFile(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9943), Application.persistentDataPath + "/testout.mp4");
            }
            catch (Exception e)
            {
                string error = e.ToString();
                Debug.LogError("ERR: " + error);
            }
        }

        private void StreamToFile(IPEndPoint server, string filePath)
        {
            string args = $"-f mpegts -probesize 32 -fflags nobuffer -flags low_delay -t 60 -i tcp://{server.Address}:{server.Port} \"{filePath}\"";

            PlatformFFMpegService service = new PlatformFFMpegService();
            service.ExecuteAsync(args);
        }
    }
}