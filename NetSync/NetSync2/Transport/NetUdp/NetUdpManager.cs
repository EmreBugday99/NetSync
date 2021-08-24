using NetSync2.Client;
using NetSync2.Server;
using System;
using System.Collections.Generic;
using System.Net;

namespace NetSync2.Transport.NetUdp
{
    public class NetUdpManager : TransportBase
    {
        internal ushort PacketSize;
        internal int ClientPort;
        internal int ServerPort;

        internal NetUdpListener Listener;
        internal IPEndPoint ServerEndPoint;
        internal NetUdpSender Sender;

        internal Network NetManager;
        internal NetServer Server;
        internal NetClient Client;

        internal List<Tuple<RemoteHandle, Packet>> RpcBuffer;
        internal object RpcBufferLock;

        public NetUdpManager(string serverIp, int serverPort, int clientPort)
        {
            RpcBuffer = new List<Tuple<RemoteHandle, Packet>>();
            RpcBufferLock = new object();

            Listener = null;
            Sender = null;
            Server = null;
            Client = null;

            ServerEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            ClientPort = clientPort;
            ServerPort = serverPort;
        }

        public override void StartClient(NetClient client)
        {
            NetManager = client.NetManager;
            Client = client;
            PacketSize = NetManager.PacketSize;

            if (Listener == null)
                Listener = new NetUdpListener(this);
            if (Sender == null)
                Sender = new NetUdpSender(this);

            Listener.StartListening(ClientPort);
        }

        public override void DisconnectClient(NetClient client)
        {
        }

        public override void StartServer(NetServer server)
        {
            NetManager = server.NetManager;
            Server = server;
            PacketSize = NetManager.PacketSize;

            if (Listener == null)
                Listener = new NetUdpListener(this);
            if (Sender == null)
                Sender = new NetUdpSender(this);

            Listener.StartListening(ServerPort);
        }

        public override void ServerTerminateConnection(NetConnection connection)
        {
        }

        public override void ServerStop(NetServer server)
        {
        }

        public override void SendRpc(RemoteHandle handle, ref Packet packet)
        {
            Sender.InvokeRpc(handle, ref packet);
        }

        public void ExecuteRpcBuffer()
        {
            if (RpcBuffer.Count == 0)
                return;

            lock (RpcBufferLock)
            {
                for (int i = RpcBuffer.Count - 1; i >= 0; i--)
                {
                    Packet packet = RpcBuffer[i].Item2;
                    RpcBuffer[i].Item1.RpcHandle.Invoke(ref packet);
                }

                RpcBuffer.Clear();
            }
        }
    }
}