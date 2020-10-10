using NetSync.Transport;

namespace NetSync.Server
{
    public class Connection
    {
        public readonly ushort ConnectionId;
        public bool IsConnected;
        public readonly NetworkServer ServerInstance;
        internal TransportBase Transport;

        public Connection(ushort id, NetworkServer serverInstance)
        {
            ServerInstance = serverInstance;
            IsConnected = false;
            ConnectionId = id;
        }

        /// <summary>
        /// Disconnects / kicks the client from network.
        /// </summary>
        public void Disconnect()
        {
            IsConnected = false;
            ServerInstance.Transport.ServerDisconnect(this);
        }
    }
}