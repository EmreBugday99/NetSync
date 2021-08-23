using System.Net;

namespace NetSync2
{
    public class NetConnection
    {
        public readonly Network NetManager;

        public readonly ushort ConnectionId;
        /// <summary>
        /// Network Unique Identifier
        /// Ex: Steam Name / Epic Name / User Name
        /// </summary>
        public string NUID;
        public IPEndPoint EndPoint;
        private bool _isConnected;

        public NetConnection(ushort id, Network netManager)
        {
            NetManager = netManager;

            NUID = null;
            _isConnected = false;
            ConnectionId = id;
        }

        /// <summary>
        /// Is this NetConnection currently connected to any network?
        /// </summary>
        public bool IsConnected()
        {
            return _isConnected;
        }

        /// <summary>
        /// Disconnects / kicks the client from network.
        /// </summary>
        public void Disconnect()
        {
            _isConnected = false;
            NUID = null;
        }
    }
}