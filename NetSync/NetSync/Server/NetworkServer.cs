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
        internal readonly TransportBase Transport;
        private bool _isActive;
        internal Connection[] Connections;

        public delegate void MessageHandle(Connection connection, Packet packet);
        private Dictionary<PacketHeader, ServerHandle> ReceiveHandlers = new Dictionary<PacketHeader, ServerHandle>();

        /// <summary>
        /// Server side handler queue for single threaded applications.
        /// </summary>
        private List<ServerQueueHandle> _serverHandleQueue = new List<ServerQueueHandle>();
        /// <summary>
        /// The lock object that should be used for reading/modifying Server Handle Queue
        /// </summary>
        internal object QueueLock = new object();

        /// <summary>
        /// Network synced objects
        /// </summary>
        private List<object> _networkedObjects = new List<object>();

        #region Events

        public delegate void NetworkServerStarted(NetworkServer server);
        /// <summary>
        /// Called after server started.
        /// </summary>
        public event NetworkServerStarted OnServerStarted;

        public delegate void NetworkServerConnected(Connection connection);
        /// <summary>
        /// Called after server receives establishes a new connection with a client.
        /// </summary>
        public event NetworkServerConnected OnServerConnected;

        public delegate void NetworkServerDisconnected(Connection connection);
        /// <summary>
        /// Called after server closes a connection with a client.
        /// </summary>
        public event NetworkServerDisconnected OnServerDisconnected;

        public delegate void NetworkServerStopped(NetworkServer server);
        /// <summary>
        /// Called after server stops.
        /// </summary>
        public event NetworkServerStopped OnServerStopped;

        public delegate void NetworkServerError(string description);
        /// <summary>
        /// Called after Server throws/detects an error.
        /// </summary>
        public event NetworkServerError OnServerErrorDetected;

        public delegate void NetworkServerSuccessfulHandshake(Connection connection);
        /// <summary>
        /// Called after server successfully finishes the handshake process with a client.
        /// </summary>
        public event NetworkServerSuccessfulHandshake OnServerHandshake;

        #endregion Events

        #region Startup / Initialization

        public NetworkServer(int serverPort, ushort maxConnections, int dataBufferSize, TransportBase transport)
        {
            _isActive = false;
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

            RegisterHandler((byte)Packets.Handshake, ServerHandshakeReceived, 0);
        }

        /// <summary>
        /// Starts the server and begins listening for new connections.
        /// </summary>
        /// <param name="threadPriority"></param>
        public void StartServer()
        {
            Transport.OnServerStarted += ServerStarted;
            Transport.OnServerConnected += ServerConnected;
            Transport.OnServerDataReceived += ServerDataReceived;
            Transport.OnServerDisconnected += ServerDisconnected;
            Transport.OnServerStopped += ServerStopped;
            Transport.OnServerError += OnServerError;

            Transport.ServerStart(this);
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            Transport.ServerStop();
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

        #region Server Events

        /// <summary>
        /// When server gets started and begins listening for new connections.
        /// </summary>
        /// <param name="server">The NetworkServer that started.</param>
        private void ServerStarted(NetworkServer server)
        {
            _isActive = true;
            OnServerStarted?.Invoke(server);
        }

        /// <summary>
        /// When a new client joins the server.
        /// </summary>
        /// <param name="connection">Connection class this client is using/utilizing.</param>
        private void ServerConnected(Connection connection)
        {
            connection.IsConnected = true;
            connection.HandshakeCompleted = false;

            //Handling the handshake with client.
            Packet handshakePacket = new Packet();
            //Informing client regarding it's connection ID.
            handshakePacket.WriteUnsignedShort(connection.ConnectionId);
            NetworkSend(connection, (byte)Packets.Handshake, handshakePacket, 0);

            OnServerConnected?.Invoke(connection);
        }

        /// <summary>
        /// When Server received a data from a client.
        /// </summary>
        /// <param name="connection">Connection/Client who send the data.</param>
        /// <param name="packet">Packet server received.</param>
        /// <param name="packetHeader">NetSync Packet Header of the packet server received.</param>
        private void ServerDataReceived(Connection connection, Packet packet, PacketHeader packetHeader)
        {
            //If the client did not complete the initial handshake and the packet they send is not a handshake packet refuse connection.
            if (connection.HandshakeCompleted == false && packetHeader.Channel != 0 && packetHeader.PacketId != (byte)Packets.Handshake)
            {
                connection.Disconnect();
                return;
            }

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

        /// <summary>
        /// When a client disconnects from server.
        /// </summary>
        /// <param name="connection">Connection/Client that disconnected from server.</param>
        private void ServerDisconnected(Connection connection)
        {
            connection.IsConnected = false;
            OnServerDisconnected?.Invoke(connection);
        }

        /// <summary>
        /// When server stops completely.
        /// </summary>
        /// <param name="server"></param>
        private void ServerStopped(NetworkServer server)
        {
            _isActive = false;
            OnServerStopped?.Invoke(server);
        }

        /// <summary>
        /// When server detects an error.
        /// </summary>
        /// <param name="description">Description of the error.</param>
        private void OnServerError(string description)
        {
            Console.WriteLine("Error: " + description);
            OnServerErrorDetected?.Invoke(description);
        }

        #endregion Server Events

        /// <summary>
        /// Checks if the server is up and running / listening for new connections.
        /// </summary>
        /// <returns>Returns true if server is running.</returns>
        public bool IsServerActive()
        {
            return _isActive;
        }

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

        /// <summary>
        /// Executed after server receives handshake packet from client
        /// </summary>
        /// <param name="connection">Client that send the packet</param>
        /// <param name="packet">Handshake packet</param>
        private void ServerHandshakeReceived(Connection connection, Packet packet)
        {
            ushort connectionId = packet.ReadUnsignedShort();
            //If client fails the handshake we will refuse the connection.
            if (connection.ConnectionId == connectionId)
            {
                connection.HandshakeCompleted = true;

                Packet handshakePacket = new Packet();
                handshakePacket.WriteUnsignedShort(connectionId);

                NetworkSend(connection, (byte)Packets.SuccessfulHandshake, handshakePacket, 0);
                OnServerHandshake?.Invoke(connection);

                LateComerNetworkObjectSync(connection);
            }
            else
                connection.Disconnect();
        }

        #region Network Object Handling

        /// <summary>
        /// Creates a networked object for everyone
        /// </summary>
        /// <param name="objectToCreate">What class/object to create on all clients</param>
        /// <param name="lateJoinerSynced">Will this object also get created for late comers</param>
        public void CreateNetworkObject(object objectToCreate, bool lateJoinerSynced = false)
        {
            string typeName = objectToCreate.GetType().AssemblyQualifiedName;
            Packet packet = new Packet();
            packet.WriteString(typeName);
            NetworkSendEveryone((byte)Packets.SyncNetworkObject, packet, 0);

            //Adding the object to the NetworkObjects list for late comer synchronization
            if (lateJoinerSynced && !_networkedObjects.Contains(objectToCreate))
                _networkedObjects.Add(objectToCreate);
        }

        /// <summary>
        /// Creates a networked object for a specific client
        /// </summary>
        /// <param name="connection">Client's connection</param>
        /// <param name="objectToCreate">Which networked object/class to create</param>
        /// <param name="lateJoinerSynced">Will this object also get created for late comers</param>
        public void CreateNetworkObject(Connection connection, object objectToCreate, bool lateJoinerSynced = false)
        {
            string typeName = objectToCreate.GetType().AssemblyQualifiedName;
            Packet packet = new Packet();
            packet.WriteString(typeName);
            NetworkSend(connection, (byte)Packets.SyncNetworkObject, packet, 0);

            //Adding the object to the NetworkObjects list for late comer synchronization
            if (lateJoinerSynced && !_networkedObjects.Contains(objectToCreate))
                _networkedObjects.Add(objectToCreate);
        }

        /// <summary>
        /// Synchronizes the already registered network objects for the late comer.
        /// </summary>
        /// <param name="connection">Late comer client</param>
        private void LateComerNetworkObjectSync(Connection connection)
        {
            //If there is any NetworkObjects in the list create them as well for the new client(late joiner).
            if (_networkedObjects.Count > 0)
            {
                foreach (var networkedObject in _networkedObjects)
                {
                    CreateNetworkObject(connection, networkedObject);
                }
            }
        }

        /// <summary>
        /// Removes a networked object from NetworkObjects list(late comer synced).
        /// </summary>
        /// <param name="objectToRemove">Object/Class to remove.</param>
        public void RemoveNetworkObject(object objectToRemove)
        {
            if (_networkedObjects.Contains(objectToRemove))
            {
                Console.WriteLine("Removed");
                _networkedObjects.Remove(objectToRemove);
                return;
            }

            OnServerError("Network Object: Can't remove a non-registered object!");
        }

        #endregion Network Object Handling
    }
}
