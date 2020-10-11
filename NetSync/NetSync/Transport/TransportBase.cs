using NetSync.Client;
using NetSync.Server;

namespace NetSync.Transport
{
    public abstract class TransportBase
    {
        #region Client

        public delegate void ClientConnected();
        public event ClientConnected OnClientConnected;

        public delegate void ClientDataReceived(Packet packet, byte channel);
        public event ClientDataReceived OnClientDataReceived;

        public delegate void ClientDisconnected();
        public event ClientDisconnected OnClientDisconnected;

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

        public delegate void ServerStarted(NetworkServer server);
        public event ServerStarted OnServerStarted;

        public delegate void ServerConnected(Connection connection);
        public event ServerConnected OnServerConnected;

        public delegate void ServerDataReceived(Connection connection, Packet packet, byte channel);
        public event ServerDataReceived OnServerDataReceived;

        public delegate void ServerDisconnected(Connection connection);

        public event ServerDisconnected OnServerDisconnected;

        public delegate void ServerStopped(NetworkServer server);

        public event ServerStopped OnServerStopped;

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