using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Unity.Collections;
using UnityEngine;

public class VideoStreamClient : MonoBehaviour
{
    public Texture2D VideoTexture { get; } = null!;
    private UdpClient m_client;

    // public bool TryReadFrameData(out ReceivedFrameData frameData)
    // {
    //     return m_frameQueue.TryDequeue(out frameData);
    // }

    private void Start()
    {
        new Thread(StartClient).Start();
    }

    private void StartClient()
    {
        m_client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 1562));

        byte[] connectionData = new byte[1] { 0 };
        m_client.Send(connectionData, connectionData.Length, new IPEndPoint(IPAddress.Loopback, 1561));

        Debug.Log("Connected");

        IPEndPoint remoteEndPoint = null;
        while (!Application.exitCancellationToken.IsCancellationRequested)
        {
            byte[] frameIndexData = m_client.Receive(ref remoteEndPoint);
            byte[] frameEyeData = m_client.Receive(ref remoteEndPoint);
            byte[] data = m_client.Receive(ref remoteEndPoint);
            // m_frameQueue.Enqueue(new ReceivedFrameData(data, BitConverter.ToInt32(frameIndexData), frameEyeData[0] == 1));

            Debug.Log($"Frame data received {frameIndexData.Length}, {frameEyeData.Length}, {data.Length}");
        }
    }
}