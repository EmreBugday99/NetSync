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
        internal Dictionary<ushort, MessageHandle> ReceiveHandlers = new Dictionary<ushort, MessageHandle>();

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

            RegisterHandler(1, ClientSyncNetworkObject);
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
            foreach (var (key, value) in ReceiveHandlers)
            {
                if (value.Method.Name == handler.Method.Name)
                    ReceiveHandlers.Remove(key);
            }
        }

        #endregion Startup / Initialization

        public void NetworkSend(ushort packetId, Packet packet, byte channel = 0)
        {
            packet.InsertUnsignedShort(0, packetId);
            Transport.ClientSendData(packet, channel);
        }

        #region Transport Events

        private void ClientConnected()
        {
        }

        private void ClientDataReceived(Packet packet, byte channel)
        {
            ushort packetId = packet.ReadUnsignedShort();
            ReceiveHandlers[packetId](packet);
        }

        private void ClientDisconnected()
        {
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