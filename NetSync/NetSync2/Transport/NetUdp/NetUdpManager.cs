using System.Net;

namespace NetSync2.Transport.NetUdp
{
    public class NetUdpManager : TransportBase
    {
        internal ushort PacketSize;
        internal int ClientPort;
        internal int ServerPort;

        internal NetUdpListener ServerListener;
        internal NetUdpListener ClientListener;

        internal NetUdpSender Sender;

        internal IPEndPoint ServerEndPoint;

        internal Network NetManager;
        internal NetServer Server;
        internal NetClient Client;

        public NetUdpManager(string serverIp, int serverPort, int clientPort)
        {
            ClientListener = null;
            ServerListener = null;
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

            ClientListener = new NetUdpListener(this);
            ClientListener.ServerListener = false;
            if (Sender == null)
                Sender = new NetUdpSender(this);

            ClientListener.StartListening(ClientPort);
        }

        public override void DisconnectClient(NetClient client)
        {
        }

        public override void StartServer(NetServer server)
        {
            NetManager = server.NetManager;
            Server = server;
            PacketSize = NetManager.PacketSize;

            ServerListener = new NetUdpListener(this);
            ServerListener.ServerListener = true;
            if (Sender == null)
                Sender = new NetUdpSender(this);

            ServerListener.StartListening(ServerPort);
        }

        public override void ServerTerminateConnection(NetConnection connection)
        {
        }

        public override void ServerStop(NetServer server)
        {
        }

        public override void SendMessageToServer(ref Packet packet)
        {
            Sender.SendMessage(ref packet, ServerEndPoint);
        }

        public override void SendMessageToClient(ref Packet packet, NetConnection connection)
        {
            Sender.SendMessage(ref packet, connection.EndPoint);
        }
    }
}