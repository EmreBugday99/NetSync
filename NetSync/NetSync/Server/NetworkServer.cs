using NetSync.Transport;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NetSync.Server
{
    public class NetworkServer
    {
        public Thread ServerThread;

        private readonly ushort _maxConnections;
        internal readonly int ServerPort;
        internal readonly int DataBufferSize;
        internal TransportBase Transport;
        public bool IsActive;

        //public Dictionary<int, Connection> Connections = new Dictionary<int, Connection>();
        public Connection[] Connections;

        public delegate void MessageHandle(Connection connection, Packet packet);

        internal Dictionary<ushort, MessageHandle> ReceiveHandlers = new Dictionary<ushort, MessageHandle>();

        public NetworkServer(int serverPort, ushort maxConnections, int dataBufferSize)
        {
            ServerPort = serverPort;
            _maxConnections = maxConnections;
            DataBufferSize = dataBufferSize;
            InitializeServerData();
        }

        private void InitializeServerData()
        {
            Connections = new Connection[_maxConnections];
            for (ushort i = 0; i < Connections.Length; i++)
            {
                Connections[i] = new Connection(i, this);
            }
        }

        public void Start(ThreadPriority threadPriority, TransportBase transport)
        {
            Transport = transport;

            Transport.OnServerStarted += ServerStarted;
            Transport.OnServerConnected += ServerConnected;
            Transport.OnServerDataReceived += ServerDataReceived;
            Transport.OnServerDisconnected += ServerDisconnected;
            Transport.OnServerStopped += ServerStopped;

            ServerThread = new Thread(StartServer) { Priority = threadPriority };
            ServerThread.Start();
        }

        private void StartServer()
        {
            Transport.ServerStart(this);
        }

        public void RegisterHandler(ushort handleId, MessageHandle handler)
        {
            if (ReceiveHandlers.TryAdd(handleId, handler) == false)
            {
                throw new Exception($"Error while registering handle: {handler.Method.Name}");
            }
        }

        public void RemoveHandler(MessageHandle handler)
        {
            foreach (var handle in ReceiveHandlers)
            {
                if (handle.Value.Method.Name == handler.Method.Name)
                    ReceiveHandlers.Remove(handle.Key);
            }
        }

        public void NetworkSendEveryone(ushort packetId, Packet packet, byte channel = 0)
        {
            packet.InsertUnsignedShort(0, packetId);
            foreach (var connection in Connections)
            {
                Transport.ServerSend(connection, packet, channel);
            }
        }

        public void NetworkSendMany(Connection[] connections, ushort packetId, Packet packet, byte channel = 0)
        {
            packet.InsertUnsignedShort(0, packetId);
            foreach (var connection in connections)
            {
                Transport.ServerSend(connection, packet, channel);
            }
        }

        public void NetworkSend(Connection connection, ushort packetId, Packet packet, byte channel = 0)
        {
            packet.InsertUnsignedShort(0, packetId);
            Transport.ServerSend(connection, packet, channel);
        }

        public void NetworkSend(ushort connectionId, ushort packetId, Packet packet, byte channel = 0)
        {
            packet.InsertUnsignedShort(0, packetId);
            Connection connection = Connections[connectionId];
            Transport.ServerSend(connection, packet, channel);
        }

        private void ServerStarted(NetworkServer server)
        {
        }

        private void ServerConnected(Connection connection)
        {
        }

        private void ServerDataReceived(Connection connection, Packet packet, byte channel)
        {
            ushort packetId = packet.ReadUnsignedShort();
            ReceiveHandlers[packetId](connection, packet);
        }

        private void ServerDisconnected(Connection connection)
        {
        }

        private void ServerStopped(NetworkServer server)
        {
        }
    }
}