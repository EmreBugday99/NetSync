using NetSync2.Client;
using System;
using System.Net;
using System.Net.Sockets;

namespace NetSync2.Transport.NetUdp
{
    internal class NetUdpListener
    {
        internal Socket ListenerSocket;
        internal IPEndPoint LocalEndPoint;

        private NetUdpManager _netUdp;
        private Network _network;

        private byte[] receiveBuffer;

        public NetUdpListener(NetUdpManager netUdp)
        {
            _netUdp = netUdp;
            _network = netUdp.NetManager;

            ListenerSocket = null;
            LocalEndPoint = null;
            receiveBuffer = new byte[netUdp.PacketSize];
        }

        internal void StartListening(int port)
        {
            ListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            LocalEndPoint = new IPEndPoint(IPAddress.Any, port);
            receiveBuffer = new byte[_netUdp.PacketSize];

            try
            {
                ListenerSocket.Bind(LocalEndPoint);
                ReceivePackets();
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

            while (true)
            {
                receiveSize = ListenerSocket.Receive(receiveBuffer);
                if (receiveSize > _netUdp.PacketSize)
                    continue;

                Packet packet = new Packet(receiveBuffer);

                int rpcHash = packet.ReadInteger();
                RemoteHandle handle = _network.GetHandleWithHash(rpcHash);

                if (handle.Target == TargetType.NetServer && _network.NetworkServer != null)
                    handle.RpcHandle.Invoke(ref packet);
                else if (handle.Target == TargetType.NetClient && _network.NetworkClient != null)
                    handle.RpcHandle.Invoke(ref packet);

                Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
            }
        }
    }
}