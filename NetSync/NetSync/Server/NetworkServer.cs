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

        internal Dictionary<ushort, MessageHandle> ReceiveHandlers = new Dictionary<ushort, MessageHandle>();

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

        #endregion Startup / Initialization

        #region Network Send

        public void NetworkSendEveryone(ushort packetId, Packet packet, byte channel = 0)
        {
            foreach (var connection in Connections)
            {
                NetworkSend(connection, packetId, packet, channel);
            }
        }

        public void NetworkSendMany(Connection[] connections, ushort packetId, Packet packet, byte channel = 0)
        {
            foreach (var connection in connections)
            {
                NetworkSend(connection, packetId, packet, channel);
            }
        }

        public void NetworkSend(Connection connection, ushort packetId, Packet packet, byte channel = 0)
        {
            if (connection.IsConnected == false) return;

            //TODO: FIX THIS MESS HERE! WE SHOULD NOT ALLOCATE 2 PACKETS FOR SENDING.
            //TODO: UNNECESSARY EXTRA ALLOCATION!
            Packet newPacket = new Packet();
            newPacket.WriteByteArray(packet.GetByteArray());
            newPacket.InsertUnsignedShort(0, packetId);
            Transport.ServerSend(connection, newPacket, channel);
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

        private void ServerDataReceived(Connection connection, Packet packet, byte channel)
        {
            Console.WriteLine("a1");
            ushort packetId = packet.ReadUnsignedShort();
            Console.WriteLine("a2");
            ReceiveHandlers[packetId](connection, packet);
            Console.WriteLine("a3");
        }

        private void ServerDisconnected(Connection connection)
        {
            connection.IsConnected = false;
        }

        private void ServerStopped(NetworkServer server)
        {
            IsActive = false;
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

        #endregion Network Object Handling
    }
}