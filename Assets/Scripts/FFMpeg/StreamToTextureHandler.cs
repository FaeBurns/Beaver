using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;

namespace FFMpeg
{
    public class StreamToTextureHandler
    {
        private const int MAX_LOOP_COUNT = 500;
        private const int MAX_READY_FRAME_COUNT = 2;

        private readonly Stream m_inputStream;
        private readonly RenderTexture m_texture;
        private readonly int m_width;
        private readonly int m_height;
        private readonly int m_frameBufferSize;

        private readonly byte[] m_nullBuffer;

        private readonly AutoResetEvent m_frameEvent = new AutoResetEvent(false);

        private readonly Queue<Texture2D> m_freeTextureQueue = new Queue<Texture2D>();

        // every time something is removed from one of these queues it must be added back to its sibling
        private readonly Queue<byte[]> m_freeFramebufferQueue = new Queue<byte[]>();
        private readonly Queue<byte[]> m_readyFramebufferQueue = new Queue<byte[]>();

        public StreamToTextureHandler(Stream inputStream, RenderTexture texture, int width, int height)
        {
            m_inputStream = inputStream;
            m_texture = texture;
            m_width = width;
            m_height = height;

            // Debug.Log($"Creating input framebuffer of {(width * height * 4):N0} bytes");
            m_frameBufferSize = width * height * 4; // 4 bytes per pixel

            m_nullBuffer = new byte[m_frameBufferSize];

            new Thread(UpdateFromPipeLoop)
            {
                Name = "StreamToBufferThread",
                IsBackground = true,
            }.Start();
        }

        public void OnUpdate()
        {
            m_frameEvent.Set();

            // return;
            // dequeue until only the most recent frame is available
            // TODO: check if this is actually beneficial?
            // may be better to leave the frames there and just consume them in a case where incoming framerate drops
            if (m_readyFramebufferQueue.Count > 1)
            {
                // dequeue framebuffer and add it back to the free queue
                byte[] frameBuffer = m_readyFramebufferQueue.Dequeue();
                lock (m_freeFramebufferQueue)
                    m_freeFramebufferQueue.Enqueue(frameBuffer);
            }

            // if there is an available frame - upload it
            if (m_readyFramebufferQueue.Count > 0)
            {
                byte[] framebuffer = m_readyFramebufferQueue.Dequeue();
                Texture2D uploadTarget = GetUploadTarget();

                EmplaceBufferIntoTexture(uploadTarget, framebuffer);

                // Graphics.CopyTexture(uploadTarget, m_texture);
                Graphics.Blit(uploadTarget, m_texture);

                // re-add texture to free queue once done with it
                m_freeTextureQueue.Enqueue(uploadTarget);

                // once texture is done upload it to the free queue
                lock (m_freeFramebufferQueue)
                    m_freeFramebufferQueue.Enqueue(framebuffer);
            }
        }

        private void UpdateFromPipeLoop()
        {
            m_frameEvent.WaitOne();
            while (!Application.exitCancellationToken.IsCancellationRequested)
            {
                // disabled in an attempt to increase framerate
                m_frameEvent.WaitOne();
                ReadFrameFromStream();
            }
        }

        /// <summary>
        /// Reads a frame from the stream.
        /// </summary>
        /// <threads>Safe.</threads>
        private void ReadFrameFromStream()
        {
            if (m_inputStream is NamedPipeServerStream namedPipeServerStream && !namedPipeServerStream.IsConnected)
                return;

            Stopwatch stopwatch = Stopwatch.StartNew();

            // Debug.Log("Begin Frame Get");


            // skip frame if there's already enough in the queue
            // read outside of lock here - Count is atomic so this is thread safe
            // does not matter if the count is out of date - it's better to not block the main thread from accessing anyway
            if (m_readyFramebufferQueue.Count >= MAX_READY_FRAME_COUNT)
            {
                Debug.Log("Ready frame buffer queue is full");
                // ensure that a frame is still read from the input - just don't do anything with it
                int readAmount = 0;
                while(readAmount < m_frameBufferSize)
                    readAmount += m_inputStream.Read(m_nullBuffer, 0, m_nullBuffer.Length);
                return;
            }

            int readThisFrame = 0;
            int i;
            byte[] frameBuffer = GetUnusedFrameBuffer();
            for (i = 0; i < MAX_LOOP_COUNT; i++)
            {
                int readCount = m_inputStream.Read(frameBuffer, readThisFrame, Math.Min(frameBuffer.Length, frameBuffer.Length - readThisFrame));
                readThisFrame += readCount;
                if (readThisFrame >= frameBuffer.Length)
                    break;

                // wait a little to allow for more data to be readable - reduces read overhead?
                // not sure anymore as currently only one loop executes
                Thread.Sleep(1);

                // Debug.Log($"Read {readCount}");

                Application.exitCancellationToken.ThrowIfCancellationRequested();
            }
            stopwatch.Stop();

            // re-queue same framebuffer
            // lock (m_freeFramebufferQueue)
            //     m_freeFramebufferQueue.Enqueue(frameBuffer);
            EnqueueFrameBuffer(frameBuffer);

            // Stopwatch gcStopwatch = Stopwatch.StartNew();
            // disable collect - allow it to be done incrementally?
            // GC.Collect();
            // GC.WaitForPendingFinalizers();
            // gcStopwatch.Stop();
            // Debug.Log($"GC took {gcStopwatch.Elapsed.TotalMilliseconds} ms");

            Debug.Log($"Frame occured reading {readThisFrame} / {frameBuffer.Length} taking {i} loops and {stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        /// <summary>
        /// Enqueue a frame buffer to be uploaded.
        /// </summary>
        /// <param name="frameBuffer">The buffer to upload.</param>
        /// <threads>Safe.</threads>
        private void EnqueueFrameBuffer(byte[] frameBuffer)
        {
            lock (m_readyFramebufferQueue)
            {
                m_readyFramebufferQueue.Enqueue(frameBuffer);
            }
        }

        /// <summary>
        /// Get a texture to upload a frame buffer to.
        /// </summary>
        /// <threads>Main Thread Only.</threads>
        /// <returns>A valid texture.</returns>
        private Texture2D GetUploadTarget()
        {
            // dequeue if available
            if (m_freeTextureQueue.Count > 0)
            {
                return m_freeTextureQueue.Dequeue();
            }
            // otherwise create a new texture
            else
            {
                return CreateUploadTexture();
            }
        }

        /// <summary>
        /// Get an empty framebufer to upload an inocming frame to.
        /// </summary>
        /// <threads>Safe.</threads>
        /// <returns>A framebuffer to upload a frame into.</returns>
        private byte[] GetUnusedFrameBuffer()
        {
            lock (m_freeFramebufferQueue)
            {
                // get frame buffer from queue
                if (m_freeFramebufferQueue.TryDequeue(out byte[] frameBuffer))
                {
                    return frameBuffer;
                }
                // or create if there aren't any available
                else
                {
                    return new byte[m_frameBufferSize];
                }
            }
        }

        /// <summary>
        /// Create a new texture to upload a framebuffer to. Should only be used if no other textures are available.
        /// </summary>
        /// <threads>Main Thread Only.</threads>
        /// <returns>A newly created texture.</returns>
        private Texture2D CreateUploadTexture()
        {
            // check mipCount and linear
            // linear dictates the color space - linear or sRGB
            // TODO: test linear vs sRGB
            // mipcount should be 0? - only want the one texture - 1 may include an extra mip
            Texture2D texture2D = new Texture2D(m_width, m_height, TextureFormat.ARGB32, false, false);
            return texture2D;
        }

        /// <summary>
        /// Loads a framebuffer into a texture and uploads it to the gpu.
        /// </summary>
        /// <param name="texture2D">The texture to upload to.</param>
        /// <param name="framebuffer">The framebuffer to upload.</param>
        /// <threads>Main Thread Only<br/>Warning: Contains long blocking actions.</threads>
        private void EmplaceBufferIntoTexture(Texture2D texture2D, byte[] framebuffer)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            // texture2D.SetPixelData(m_frameBuffer, 0, 0);
            texture2D.LoadRawTextureData(framebuffer);
            // TODO: check makeNoLongerReadable - it may have an impact on writing later - no need to copy the data?
            texture2D.Apply(false, false);


            if (Input.GetKeyDown(KeyCode.Space))
            {
                using FileStream file = File.OpenWrite($"{Guid.NewGuid()}.png");
                Debug.LogWarning($"Writing to png at {file.Name}");
                byte[] pngBytes = texture2D.EncodeToPNG();
                file.Write(pngBytes);

                int whitePixels = texture2D.GetPixels32().Count(p => p.a > 0);
                int blackPixels = texture2D.GetPixels32().Count(p => p.a == 0);
                Debug.LogWarning($"Incoming texture had {whitePixels} non black alpha pixels, with {blackPixels} black pixels");
            }

            stopwatch.Stop();
            // Debug.Log($"{nameof(EmplaceBufferIntoTexture)} took {stopwatch.Elapsed.TotalMilliseconds:N0} ms");
        }
    }
}