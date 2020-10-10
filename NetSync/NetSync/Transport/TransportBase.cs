using NetSync.Client;
using NetSync.Server;

namespace NetSync.Transport
{
    public abstract class TransportBase
    {
        #region Client

        internal delegate void ClientConnected();
        internal event ClientConnected OnClientConnected;

        internal delegate void ClientDataReceived(Packet packet, byte channel);
        internal event ClientDataReceived OnClientDataReceived;

        internal delegate void ClientDisconnected();
        internal event ClientDisconnected OnClientDisconnected;

        public abstract void ClientConnect(NetworkClient client);

        public abstract void ClientDisconnect();

        public abstract void ClientSendData(Packet packet, byte channel);

        protected void OnClientConnect()
            => OnClientConnected?.Invoke();

        protected void OnClientDataReceive(Packet packet, byte channel)
            => OnClientDataReceived?.Invoke(packet, channel);

        protected void OnClientDisconnect()
            => OnClientDisconnected?.Invoke();

        #endregion Client

        #region Server

        internal delegate void ServerStarted(NetworkServer server);
        internal event ServerStarted OnServerStarted;

        internal delegate void ServerConnected(Connection connection);
        internal event ServerConnected OnServerConnected;

        internal delegate void ServerDataReceived(Connection connection, Packet packet, byte channel);
        internal event ServerDataReceived OnServerDataReceived;

        internal delegate void ServerDisconnected(Connection connection);
        internal event ServerDisconnected OnServerDisconnected;

        internal delegate void ServerStopped(NetworkServer server);
        internal event ServerStopped OnServerStopped;

        public abstract void ServerStart(NetworkServer server);

        public abstract void ServerSend(Connection connection, Packet packet, byte channel);

        public abstract void ServerDisconnect(Connection connection);

        public abstract void ServerStop();

        protected void OnServerStart(NetworkServer server)
            => OnServerStarted?.Invoke(server);

        protected void OnServerConnect(Connection connection)
            => OnServerConnected?.Invoke(connection);

        protected void OnServerDataReceive(Connection connection, Packet packet, byte channel)
            => OnServerDataReceived?.Invoke(connection, packet, channel);

        protected void OnServerDisconnect(Connection connection)
            => OnServerDisconnected?.Invoke(connection);

        protected void OnServerStop(NetworkServer server)
            => OnServerStopped?.Invoke(server);

        #endregion Server
    }
}