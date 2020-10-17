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

        private Dictionary<PacketHeader, ServerHandle> ReceiveHandlers = new Dictionary<PacketHeader, ServerHandle>();

        private List<ServerQueueHandle> _serverHandleQueue = new List<ServerQueueHandle>();
        internal object QueueLock = new object();

        internal List<object> NetworkedObjects = new List<object>();

        #region Events

        public delegate void NetworkServerStarted(NetworkServer server);
        public event NetworkServerStarted OnServerStarted;

        public delegate void NetworkServerConnected(Connection connection);
        public event NetworkServerConnected OnServerConnected;

        public delegate void NetworkServerDisconnected(Connection connection);
        public event NetworkServerDisconnected OnServerDisconnected;

        public delegate void NetworkServerStopped(NetworkServer server);
        public event NetworkServerStopped OnServerStopped;

        public delegate void NetworkServerError(string description);
        public event NetworkServerError OnServerErrorDetected;

        #endregion Events

        #region Startup / Initialization

        public NetworkServer(int serverPort, ushort maxConnections, int dataBufferSize, TransportBase transport)
        {
            IsActive = false;
            ServerPort = serverPort;
            _maxConnections = maxConnections;
            DataBufferSize = dataBufferSize;
            InitializeServer();
            Transport = transport;
        }

        private void InitializeServer()
        {
            Connections = new Connection[_maxConnections];
            for (ushort i = 0; i < Connections.Length; i++)
            {
                Connections[i] = new Connection(i, this);
            }
        }

        public void Start(ThreadPriority threadPriority = ThreadPriority.Normal)
        {
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

        public void RegisterHandler(byte packetId, MessageHandle handler, byte channel = 1, bool queue = false)
        {
            PacketHeader packetHeader = new PacketHeader(channel, packetId);
            ServerHandle serverHandle = new ServerHandle(handler, queue);

            if (ReceiveHandlers.ContainsKey(packetHeader))
                throw new Exception($"Handler is already registered: Error while adding {handler.Method.Name}");

            ReceiveHandlers.Add(packetHeader, serverHandle);
        }

        public void RemoveHandler(MessageHandle handler)
        {
            foreach (var handle in ReceiveHandlers)
            {
                if (handle.Value.Handler.Method.Name == handler.Method.Name)
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

            packet.Dispose();
        }

        public void NetworkSendMany(Connection[] connections, byte packetId, Packet packet, byte channel = 1)
        {
            foreach (var connection in connections)
            {
                NetworkSend(connection, packetId, packet, channel);
            }

            packet.Dispose();
        }

        public void NetworkSend(Connection connection, byte packetId, Packet packet, byte channel = 1)
        {
            if (connection.IsConnected == false) return;
            Packet newPacket = new Packet();
            newPacket.WriteByteArray(packet.GetByteArray());

            PacketHeader packetHeader = new PacketHeader(channel, packetId);
            Transport.ServerSend(connection, newPacket, packetHeader);
            newPacket.Dispose();
        }

        #endregion Network Send

        #region Transport Events

        private void ServerStarted(NetworkServer server)
        {
            IsActive = true;
            OnServerStarted?.Invoke(server);
        }

        private void ServerConnected(Connection connection)
        {
            connection.IsConnected = true;
            OnServerConnected?.Invoke(connection);
        }

        private void ServerDataReceived(Connection connection, Packet packet, PacketHeader packetHeader)
        {
            if (ReceiveHandlers[packetHeader].IsQueued)
            {
                ServerQueueHandle queueHandle = new ServerQueueHandle(connection, packet, ReceiveHandlers[packetHeader]);
                lock (QueueLock)
                {
                    _serverHandleQueue.Add(queueHandle);
                }
                return;
            }

            ReceiveHandlers[packetHeader].Handler(connection, packet);
        }

        private void ServerDisconnected(Connection connection)
        {
            connection.IsConnected = false;
            OnServerDisconnected?.Invoke(connection);
        }

        private void ServerStopped(NetworkServer server)
        {
            IsActive = false;
            OnServerStopped?.Invoke(server);
        }

        private void OnServerError(string description)
        {
            Console.WriteLine("Error: " + description);
            OnServerErrorDetected?.Invoke(description);
        }

        #endregion Transport Events

        public void ExecuteHandleQueue()
        {
            lock (QueueLock)
            {
                for (int i = 0; i < _serverHandleQueue.Count; i++)
                {
                    _serverHandleQueue[i].Handle.Handler(_serverHandleQueue[i].Connection, _serverHandleQueue[i].ReceivedPacket);
                    _serverHandleQueue.RemoveAt(i);
                }
            }
        }

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