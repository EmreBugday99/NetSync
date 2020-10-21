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
        internal string UAI;
        internal bool IsConnected;
        internal bool HandshakeCompleted;
        private readonly NetworkServer _serverInstance;

        internal object ConnectionLock = new object();

        public Connection(ushort id, NetworkServer serverInstance)
        {
            _serverInstance = serverInstance;
            IsConnected = false;
            ConnectionId = id;
        }

        /// <summary>
        /// In most cases this will be the connection's remote end-point.
        /// </summary>
        /// <returns>Unique Address Identifier</returns>
        public string GetUniqueAddress()
        {
            return UAI;
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
                UAI = string.Empty;
                _serverInstance.Transport.ServerDisconnect(this);
            }
        }
    }
}