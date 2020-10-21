using NetSync.Transport;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NetSync.Client
{
    public class NetworkClient
    {
        internal readonly string ServerIp;
        internal readonly int ServerPort;
        internal readonly int DataBufferSize;
        private readonly TransportBase _transport;
        private ushort _connectionId;
        private bool _connected;
        private bool _handshakeCompleted;

        public delegate void MessageHandle(Packet packet);

        private Dictionary<PacketHeader, ClientHandle> ReceiveHandlers = new Dictionary<PacketHeader, ClientHandle>();

        private List<ClientQueueHandle> _clientQueueHandlers = new List<ClientQueueHandle>();
        internal object QueueLock = new object();

        #region Events

        public delegate void NetworkClientConnected();
        /// <summary>
        /// Called after client establishes connection with server.
        /// </summary>
        public event NetworkClientConnected OnClientConnected;

        public delegate void NetworkClientDisconnected();
        /// <summary>
        /// Called after Client disconnects from server.
        /// </summary>
        public event NetworkClientDisconnected OnClientDisconnected;

        public delegate void NetworkClientError(string description);
        /// <summary>
        /// Called after the Transport throws an error.
        /// </summary>
        public event NetworkClientError OnClientErrorDetected;

        public delegate void NetworkClientHandshakeCompleted(ushort connectionId);
        /// <summary>
        /// Called after client successfully finishes the handshake with server.
        /// </summary>
        public event NetworkClientHandshakeCompleted OnHandshakeCompleted;

        #endregion Events

        #region Startup / Initialization

        public NetworkClient(string ipAddress, int serverPort, int dataBufferSize, TransportBase transport)
        {
            _connected = false;
            ServerIp = ipAddress;
            ServerPort = serverPort;
            DataBufferSize = dataBufferSize;
            _transport = transport;
        }

        public void StartClient()
        {
            InitializeClient();
            _transport.ClientConnect(this);
        }

        public void StopClient()
        {
            _transport.ClientDisconnect();
        }

        private void InitializeClient()
        {
            _transport.OnClientConnected += ClientConnected;
            _transport.OnClientDataReceived += ClientDataReceived;
            _transport.OnClientDisconnected += ClientDisconnected;
            _transport.OnClientError += OnClientError;

            RegisterHandler((byte)Packets.Handshake, ClientHandshakeReceived, 0);
            RegisterHandler((byte)Packets.SuccessfulHandshake, ClientSuccessfulHandshakeReceived, 0);
            RegisterHandler((byte)Packets.SyncNetworkObject, ClientSyncNetworkObject, 0);
        }

        public void RegisterHandler(byte packetId, MessageHandle handler, byte channel = 1, bool queue = false)
        {
            PacketHeader packetHeader = new PacketHeader(channel, packetId);
            ClientHandle clientHandle = new ClientHandle(handler, queue);
            if (ReceiveHandlers.ContainsKey(packetHeader))
                throw new Exception($"Handler is already registered: {handler.Method.Name}");

            ReceiveHandlers.Add(packetHeader, clientHandle);
        }

        public void RemoveHandler(MessageHandle handler)
        {
            foreach (var msgHandler in ReceiveHandlers)
            {
                if (msgHandler.Value.Handle.Method.Name == handler.Method.Name)
                    ReceiveHandlers.Remove(msgHandler.Key);
            }
        }

        #endregion Startup / Initialization

        public void NetworkSend(byte packetId, Packet packet, byte channel = 1)
        {
            PacketHeader packetHeader = new PacketHeader(channel, packetId);
            _transport.ClientSendData(packet, packetHeader);
            packet.Dispose();
        }

        #region Client Events

        private void ClientConnected()
        {
            _connected = true;
            OnClientConnected?.Invoke();
        }

        private void ClientDataReceived(Packet packet, PacketHeader packetHeader)
        {
            if (ReceiveHandlers[packetHeader].IsQueued)
            {
                ClientQueueHandle queueHandle = new ClientQueueHandle(packet, ReceiveHandlers[packetHeader]);
                lock (QueueLock)
                {
                    _clientQueueHandlers.Add(queueHandle);
                }
                return;
            }

            ReceiveHandlers[packetHeader].Handle(packet);
        }

        private void ClientDisconnected()
        {
            _connected = false;
            OnClientDisconnected?.Invoke();
        }

        private void OnClientError(string description)
        {
            Console.WriteLine(description);
            OnClientErrorDetected?.Invoke(description);
        }

        private void OnHandshakeDone(ushort connectionId)
            => OnHandshakeCompleted?.Invoke(connectionId);

        #endregion Client Events

        /// <summary>
        /// Checks if the client is connected to the server.
        /// </summary>
        /// <returns>Returns true if connection is active.</returns>
        public bool IsActive()
        {
            return _connected;
        }

        /// <summary>
        /// Checks if the client has successfully completed the handshake process with server.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsHandshakeSuccessful()
        {
            return _handshakeCompleted;
        }

        /// <summary>
        /// Gets this client's connection id.
        /// </summary>
        /// <returns>Connection Id</returns>
        public ushort GetConnectionId()
        {
            return _connectionId;
        }

        private void ClientHandshakeReceived(Packet packet)
        {
            ushort connectionId = packet.ReadUnsignedShort();
            _connectionId = connectionId;

            Packet handshakePacket = new Packet();
            handshakePacket.WriteUnsignedShort(connectionId);

            NetworkSend((byte)Packets.Handshake, handshakePacket, 0);
        }

        private void ClientSuccessfulHandshakeReceived(Packet packet)
        {
            _handshakeCompleted = true;
            ushort connectionId = packet.ReadUnsignedShort();
            OnHandshakeDone(connectionId);
        }

        /// <summary>
        /// Executes all the queued handlers. Useful for single threaded applications.
        /// </summary>
        public void ExecuteHandleQueue()
        {
            lock (QueueLock)
            {
                for (int i = 0; i < _clientQueueHandlers.Count; i++)
                {
                    _clientQueueHandlers[i].Handle.Handle(_clientQueueHandlers[i].ReceivedPacket);
                    _clientQueueHandlers.RemoveAt(i);
                }
            }
        }

        private void ClientSyncNetworkObject(Packet packet)
        {
            string typeName = packet.ReadString();
            Type type = Type.GetType(typeName, true);
            Activator.CreateInstance(type);
        }
    }
}