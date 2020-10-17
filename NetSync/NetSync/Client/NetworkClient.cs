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

        public delegate void MessageHandle(Packet packet);

        private Dictionary<PacketHeader, ClientHandle> ReceiveHandlers = new Dictionary<PacketHeader, ClientHandle>();

        private List<ClientQueueHandle> _clientQueueHandlers = new List<ClientQueueHandle>();
        internal object QueueLock = new object();

        #region Events

        public delegate void NetworkClientConnected();
        public event NetworkClientConnected OnClientConnected;

        public delegate void NetworkClientDisconnected();
        public event NetworkClientDisconnected OnClientDisconnected;

        public delegate void NetworkClientError(string description);
        public event NetworkClientError OnClientErrorDetected;

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

            RegisterHandler(1, ClientSyncNetworkObject, 0);
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

        #region Transport Events

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

        #endregion Transport Events

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