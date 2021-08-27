﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetSync2.Transport.NetUdp
{
    internal class NetUdpListener
    {
        internal Socket ListenerSocket;
        internal IPEndPoint LocalEndPoint;

        public bool ServerListener;

        private NetUdpManager _netUdp;
        private Network _network;

        private byte[] receiveBuffer;

        internal Thread ListenerThread;

        public NetUdpListener(NetUdpManager netUdp)
        {
            _netUdp = netUdp;
            _network = netUdp.NetManager;

            ListenerThread = null;
            ListenerSocket = null;
            LocalEndPoint = null;
            receiveBuffer = new byte[netUdp.PacketSize];
        }

        internal void StartListening(int port)
        {
            ListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //ListenerSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);

            LocalEndPoint = new IPEndPoint(IPAddress.Any, port);
            receiveBuffer = new byte[_netUdp.PacketSize];
            try
            {
                ListenerSocket.Bind(LocalEndPoint);
                ListenerThread = new Thread(ReceivePackets);
                ListenerThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void ReceivePackets()
        {
            int receiveSize = 0;

            IPEndPoint senderIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            EndPoint senderEndPoint = (EndPoint)senderIpEndPoint;

            while (true)
            {
                receiveSize = ListenerSocket.ReceiveFrom(receiveBuffer, ref senderEndPoint);
                //Console.WriteLine(senderEndPoint.ToString());
                if (receiveSize > _netUdp.PacketSize)
                    continue;

                Packet packet = new Packet(ref receiveBuffer, receiveSize);
                EndPoint point = senderEndPoint;
                Task.Run(() => AddToRpcBuffer(ref packet, point));

                Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
            }
        }

        private void AddToRpcBuffer(ref Packet packet, EndPoint senderEndPoint)
        {
        }
    }
}