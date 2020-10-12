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

        public delegate void MessageHandle(Packet packet);

        internal Dictionary<PacketHeader, MessageHandle> ReceiveHandlers = new Dictionary<PacketHeader, MessageHandle>();

        public NetworkClient(string ipAddress, int serverPort, int dataBufferSize)
        {
            ServerIp = ipAddress;
            ServerPort = serverPort;
            DataBufferSize = dataBufferSize;
        }

        #region Startup / Initialization

        public void ConnectToServer(ThreadPriority threadPriority, TransportBase transport)
        {
            Transport = transport;
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

        public void RegisterHandler(byte packetId, MessageHandle handler, byte channel = 1)
        {
            PacketHeader packetHeader = new PacketHeader(channel, packetId);
            if(ReceiveHandlers.ContainsKey(packetHeader))
                throw new Exception($"Handler is already registered: {handler.Method.Name}");

            ReceiveHandlers.Add(packetHeader, handler);
        }

        public void RemoveHandler(MessageHandle handler)
        {
            foreach (var msgHandler in ReceiveHandlers)
            {
                if (msgHandler.Value.Method.Name == handler.Method.Name)
                    ReceiveHandlers.Remove(msgHandler.Key);
            }
        }

        #endregion Startup / Initialization

        public void NetworkSend(byte packetId, Packet packet, byte channel = 1)
        {
            PacketHeader packetHeader = new PacketHeader(channel, packetId);
            Transport.ClientSendData(packet, packetHeader);
        }

        #region Transport Events

        private void ClientConnected()
        {
        }

        private void ClientDataReceived(Packet packet, PacketHeader packetHeader)
        {
            ReceiveHandlers[packetHeader](packet);
        }

        private void ClientDisconnected()
        {
        }

        private void OnClientError(string description)
        {
            throw new Exception("Client Error: " + description);
        }

        #endregion Transport Events

        private void ClientSyncNetworkObject(Packet packet)
        {
            string typeName = packet.ReadString();
            Type type = Type.GetType(typeName, true);
            Activator.CreateInstance(type);
        }
    }
}