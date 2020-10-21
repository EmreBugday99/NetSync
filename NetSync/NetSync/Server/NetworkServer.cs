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

        /// <summary>
        /// Server side handler queue for single threaded applications.
        /// </summary>
        private List<ServerQueueHandle> _serverHandleQueue = new List<ServerQueueHandle>();
        internal object QueueLock = new object();

        /// <summary>
        /// Network synced objects
        /// </summary>
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

        /// <summary>
        /// Setups the server. Runs before Start
        /// </summary>
        private void InitializeServer()
        {
            Connections = new Connection[_maxConnections];
            for (ushort i = 0; i < Connections.Length; i++)
            {
                Connections[i] = new Connection(i, this);
            }
        }

        /// <summary>
        /// Starts the server and begins listening for new connections.
        /// </summary>
        /// <param name="threadPriority"></param>
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

        /// <summary>
        /// Registers a handler for a packet.
        /// </summary>
        /// <param name="packetId">Packet Id to create a handler for</param>
        /// <param name="handler">Handler to execute when packet gets received</param>
        /// <param name="channel">Which channel this packet is using in network</param>
        /// <param name="queue">Will this handle be registered as queued (for single threaded applications)</param>
        public void RegisterHandler(byte packetId, MessageHandle handler, byte channel = 1, bool queue = false)
        {
            PacketHeader packetHeader = new PacketHeader(channel, packetId);
            ServerHandle serverHandle = new ServerHandle(handler, queue);

            if (ReceiveHandlers.ContainsKey(packetHeader))
                throw new Exception($"Handler is already registered: Error while adding {handler.Method.Name}");

            ReceiveHandlers.Add(packetHeader, serverHandle);
        }

        /// <summary>
        /// Removes a handler from registered handlers list
        /// </summary>
        /// <param name="handler">Specific handler to remove</param>
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

        /// <summary>
        /// Executes all the queued handlers. Useful for single threaded applications.
        /// </summary>
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

        /// <summary>
        /// Creates a networked object for everyone
        /// </summary>
        /// <param name="objectToCreate">What class/object to create on all clients</param>
        public void CreateNetworkObject(object objectToCreate)
        {
            string typeName = objectToCreate.GetType().AssemblyQualifiedName;
            Packet packet = new Packet();
            packet.WriteString(typeName);
            NetworkSendEveryone(1, packet);
        }

        /// <summary>
        /// Creates a networked object for a specific client
        /// </summary>
        /// <param name="connection">Client's connection</param>
        /// <param name="objectToCreate">Which networked object/class to create</param>
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