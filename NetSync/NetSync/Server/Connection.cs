using System;

namespace NetSync.Server
{
    public class Connection
    {
        public readonly ushort ConnectionId;
        /// <summary>
        /// In most cases this will be the connection ip.
        /// Stands for Unique Address Identifier
        /// </summary>
        public string UAI;
        public bool IsConnected;
        public bool HandshakeCompleted;
        public readonly NetworkServer ServerInstance;

        internal object ConnectionLock = new object();

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
            lock (ConnectionLock)
            {
                IsConnected = false;
                HandshakeCompleted = false;
                ServerInstance.Transport.ServerDisconnect(this);
            }
        }
    }
}