using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FFMpeg
{
    public class StreamToTextureHandler
    {
        private const int MAX_LOOP_COUNT = 500;

        private readonly Stream m_inputStream;
        private readonly RenderTexture m_texture;
        private readonly byte[] m_frameBuffer;

        private readonly AutoResetEvent m_frameEvent = new AutoResetEvent(false);

        public StreamToTextureHandler(Stream inputStream, RenderTexture texture, int width, int height)
        {
            m_inputStream = inputStream;
            m_texture = texture;

            Debug.Log($"Creating input framebuffer of {(width * height * 4):N0} bytes");
            m_frameBuffer = new byte[width * height * 4];

            new Thread(UpdateFromPipeLoop).Start();
        }

        public void OnUpdate()
        {
            m_frameEvent.Set();
        }

        private void UpdateFromPipeLoop()
        {
            m_frameEvent.WaitOne();
            while (!Application.exitCancellationToken.IsCancellationRequested)
            {
                m_frameEvent.WaitOne();
                UpdateFromPipe();
            }
        }

        private void UpdateFromPipe()
        {
            // if (!(m_inputStream as NamedPipeServerStream)!.IsConnected)
            //     return;

            Stopwatch stopwatch = Stopwatch.StartNew();

            Debug.Log("Begin Frame Get");
            int readThisFrame = 0;
            int i;
            for (i = 0; i < MAX_LOOP_COUNT; i++)
            {
                int readCount = m_inputStream.Read(m_frameBuffer, 0, Math.Min(m_frameBuffer.Length, m_frameBuffer.Length - readThisFrame));
                readThisFrame += readCount;
                if (readThisFrame >= m_frameBuffer.Length)
                    break;

                // wait a little to allow for more data to be readable - reduces read overhead?
                Thread.Sleep(1000);

                // Debug.Log($"Read {readCount}");

                Application.exitCancellationToken.ThrowIfCancellationRequested();
            }
            stopwatch.Stop();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Debug.Log($"Frame occured reading {readThisFrame} / {m_frameBuffer.Length} taking {i} loops and {stopwatch.Elapsed.TotalMilliseconds} ms");
        }
    }
}