using NetSync2.Client;
using NetSync2.Server;

namespace NetSync2.Transport.NetUdp
{
    public class NetUdpManager : TransportBase
    {
        internal ushort PacketSize;
        internal int ClientPort;
        internal int ServerPort;

        internal NetUdpListener Listener;
        internal NetUdpSender Sender;

        internal Network NetManager;
        internal NetServer Server;
        internal NetClient Client;

        public NetUdpManager(int serverPort, int clientPort)
        {
            ClientPort = clientPort;
            ServerPort = serverPort;

            Listener = null;
            Sender = null;
            Server = null;
            Client = null;
        }

        public override void ConnectClient(NetClient client)
        {
            NetManager = client.NetManager;
            Client = client;
            PacketSize = NetManager.PacketSize;

            //if (Listener == null)
            //  Listener = new NetUdpListener(this);
            if (Sender == null)
                Sender = new NetUdpSender(this);

            //Listener.StartListening(ClientPort);
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
    }
}