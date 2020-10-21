using NetSync.Transport;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NetSync.Client
{
    public class NetworkClient
    {
        public Thread ClientThread;

        internal readonly string ServerIp;
        internal readonly int ServerPort;
        internal int DataBufferSize;
        internal TransportBase Transport;
        public ushort ConnectionId;
        public bool IsActive;
        public bool HandshakeCompleted;

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
            IsActive = false;
            ServerIp = ipAddress;
            ServerPort = serverPort;
            DataBufferSize = dataBufferSize;
            Transport = transport;
        }

        public void ConnectToServer(ThreadPriority threadPriority = ThreadPriority.Normal)
        {
            ClientThread = new Thread(EstablishConnectionWithServer);
            ClientThread.Priority = threadPriority;
            ClientThread.Start();
        }

        private void EstablishConnectionWithServer()
        {
            InitializeClient();

            Transport.ClientConnect(this);
        }

        private void InitializeClient()
        {
            Transport.OnClientConnected += ClientConnected;
            Transport.OnClientDataReceived += ClientDataReceived;
            Transport.OnClientDisconnected += ClientDisconnected;
            Transport.OnClientError += OnClientError;

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
            Transport.ClientSendData(packet, packetHeader);
            packet.Dispose();
        }

        #region Client Events

        private void ClientConnected()
        {
            IsActive = true;
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
            IsActive = false;
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

        private void ClientHandshakeReceived(Packet packet)
        {
            ushort connectionId = packet.ReadUnsignedShort();
            ConnectionId = connectionId;

            Packet handshakePacket = new Packet();
            handshakePacket.WriteUnsignedShort(connectionId);

            NetworkSend((byte)Packets.Handshake, handshakePacket, 0);
        }

        private void ClientSuccessfulHandshakeReceived(Packet packet)
        {
            HandshakeCompleted = true;
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