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
        public Connection[] Connections;

        public delegate void MessageHandle(Connection connection, Packet packet);
        internal Dictionary<PacketHeader, MessageHandle> ReceiveHandlers = new Dictionary<PacketHeader, MessageHandle>();

        internal List<object> NetworkedObjects = new List<object>();

        #region Startup / Initialization

        public NetworkServer(int serverPort, ushort maxConnections, int dataBufferSize)
        {
            ServerPort = serverPort;
            _maxConnections = maxConnections;
            DataBufferSize = dataBufferSize;
            InitializeServer();
        }

        private void InitializeServer()
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
            Transport.OnServerError += OnServerError;

            ServerThread = new Thread(StartServer) { Priority = threadPriority };
            ServerThread.Start();
        }

        private void StartServer()
        {
            Transport.ServerStart(this);
        }

        public void RegisterHandler(byte packetId, MessageHandle handler, byte channel = 1)
        {
            PacketHeader packetHeader = new PacketHeader(channel, packetId);

            if(ReceiveHandlers.ContainsKey(packetHeader))
                throw new Exception($"Handler is already registered: {handler.Method.Name}");

            ReceiveHandlers.Add(packetHeader, handler);
        }

        public void RemoveHandler(MessageHandle handler)
        {
            foreach (var handle in ReceiveHandlers)
            {
                if (handle.Value.Method.Name == handler.Method.Name)
                    ReceiveHandlers.Remove(handle.Key);
            }
        }

        #endregion Startup / Initialization

        #region Network Send

        public void NetworkSendEveryone(byte packetId, Packet packet, byte channel = 1)
        {
            foreach (var connection in Connections)
            {
                NetworkSend(connection, packetId, packet, channel);
            }
        }

        public void NetworkSendMany(Connection[] connections, byte packetId, Packet packet, byte channel = 1)
        {
            foreach (var connection in connections)
            {
                NetworkSend(connection, packetId, packet, channel);
            }
        }

        public void NetworkSend(Connection connection, byte packetId, Packet packet, byte channel = 1)
        {
            if (connection.IsConnected == false) return;

            //TODO: FIX THIS MESS HERE! WE SHOULD NOT ALLOCATE 2 PACKETS FOR SENDING.
            //TODO: UNNECESSARY EXTRA ALLOCATION!
            Packet newPacket = new Packet();
            newPacket.WriteByteArray(packet.GetByteArray());

            PacketHeader packetHeader = new PacketHeader(channel, packetId);
            Transport.ServerSend(connection, newPacket, packetHeader);
        }

        #endregion Network Send

        #region Transport Events

        private void ServerStarted(NetworkServer server)
        {
            IsActive = true;
        }

        private void ServerConnected(Connection connection)
        {
            connection.IsConnected = true;
        }

        private void ServerDataReceived(Connection connection, Packet packet, PacketHeader packetHeader)
        {
            ReceiveHandlers[packetHeader](connection, packet);
        }

        private void ServerDisconnected(Connection connection)
        {
            connection.IsConnected = false;
        }

        private void ServerStopped(NetworkServer server)
        {
            IsActive = false;
        }

        private void OnServerError(string description)
        {
            throw new Exception("Error: " + description);
        }

        #endregion Transport Events

        #region Network Object Handling

        public void CreateNetworkObject(object objectToCreate)
        {
            string typeName = objectToCreate.GetType().AssemblyQualifiedName;
            Packet packet = new Packet();
            packet.WriteString(typeName);
            NetworkSendEveryone(1, packet);
        }

        public void CreateNetworkObjectForClient(Connection connection, object objectToCreate)
        {
            string typeName = objectToCreate.GetType().AssemblyQualifiedName;
            Packet packet = new Packet();
            packet.WriteString(typeName);
            NetworkSend(connection, 1, packet);
        }

        #endregion Network Object Handling
    }
}