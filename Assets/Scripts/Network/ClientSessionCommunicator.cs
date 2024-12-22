using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Network
{
    public class ClientSessionCommunicator : IDisposable
    {
        private TcpClient m_client;

        public async Task ConnectAsync(int port)
        {
            TcpClient client = new TcpClient();

            // connect on loopback for adb
            // port 9944 for control signals
            await client.ConnectAsync(IPAddress.Loopback, port);

            // set client on self
            m_client = client;
        }

        public async Task<bool> NegotiateConnectionAsync(int width, int height, float framerate)
        {
            ConnectionCheck();

            // get read/write streams
            NetworkStream stream = m_client.GetStream();
            using StreamReader sr = new StreamReader(stream);

            // write negotiate command
            // ReSharper disable once MethodHasAsyncOverload - not required here
            stream.Write(BitConverter.GetBytes((int)CommandType.NEGOTIATE_STREAM));
            stream.Write(BitConverter.GetBytes(width));
            stream.Write(BitConverter.GetBytes(height));
            stream.Write(BitConverter.GetBytes(framerate));
            await stream.FlushAsync();

            // read single character into return buffer
            char[] returnBuffer = new char[1];
            await sr.ReadAsync(returnBuffer, 0, returnBuffer.Length);

            // check return character to see if negotiation was a success
            return (byte)returnBuffer[0] > 0;
        }

        public async Task InformDisconnectAsync()
        {
            ConnectionCheck();

            await using StreamWriter sw = new StreamWriter(m_client.GetStream());
            sw.AutoFlush = false;
            sw.Write((byte)CommandType.EXIT);
            await sw.FlushAsync();
        }

        private void ConnectionCheck()
        {
            if (m_client == null) throw new InvalidOperationException("Client is not connected");
        }

        public void Dispose()
        {
            m_client?.Dispose();
        }
    }
}