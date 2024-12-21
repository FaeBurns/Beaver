using System;
using System.Collections;
using System.IO;
using System.Net;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;
using Debug = UnityEngine.Debug;

namespace FFMpeg
{
    public class FFMpegStreamReceiver : MonoBehaviour
    {
        [CanBeNull]
        private StreamToTextureHandler m_streamToTextureHandler;

        [SerializeField]
        private RenderTexture m_renderTexture;

        [SerializeField]
        private int m_width;
        [SerializeField]
        private int m_height;

        [FormerlySerializedAs("m_framerate")]
        [SerializeField]
        private int m_frameRate;

        private void Awake()
        {
            File.Delete(Application.persistentDataPath + "/log.txt");
            Application.logMessageReceived += ApplicationOnlogMessageReceived;
        }

        private void Start()
        {
            StartCoroutine(SetupDelay(5));
        }

        private IEnumerator SetupDelay(int delay)
        {
            for (int i = 0; i < delay; i++)
            {
                yield return null;
            }

            Setup();
        }

        private void Setup()
        {
            // if (File.Exists(Application.persistentDataPath + "/testout.mp4"))
            //     File.Delete(Application.persistentDataPath + "/testout.mp4");

            try
            {
                // Debug.Log(Application.persistentDataPath + "/testout.mp4");
                // StreamToFile(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9943), Application.persistentDataPath + "/testout.mp4");
                QualitySettings.vSyncCount = 0;
                Debug.Log($"Framerate: {Application.targetFrameRate}, Resolution: {XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight}");
                Application.targetFrameRate = m_frameRate;

                PlatformFFMpegService service = new PlatformFFMpegService();
                Stream streamInputPipe = service.OpenStreamServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9943), m_width, m_height, m_frameRate);

                m_streamToTextureHandler = new StreamToTextureHandler(streamInputPipe, m_renderTexture, m_width, m_height);
            }
            catch (Exception e)
            {
                string error = e.ToString();
                Debug.LogError("ERR: " + error);
            }
        }

        private void StreamToFile(IPEndPoint server, string filePath)
        {
            string args = $"-f mpegts -probesize 32 -fflags nobuffer -flags low_delay -t 60 -i tcp://{server.Address}:{server.Port} \"{filePath}\" -y";

            PlatformFFMpegService service = new PlatformFFMpegService();
            service.ExecuteAsync(args);
        }

        private void Update()
        {
            m_streamToTextureHandler?.OnUpdate();
        }

        private static void ApplicationOnlogMessageReceived(string condition, string stacktrace, LogType type)
        {
            using StreamWriter sw = new StreamWriter(Application.persistentDataPath + "/log.txt", true);
            sw.WriteLine($"[{type}] | {condition}");
        }
    }
}