using System.Net;

namespace NetSync2
{
    public class NetConnection
    {
        public readonly Network NetManager;

        public ushort ConnectionId;

        /// <summary>
        /// Network Unique Identifier
        /// Ex: User Name / Steam Name / Epic Name etc.
        /// </summary>
        public string NUID;

        public IPEndPoint EndPoint;

        /// <summary>
        /// Is this NetConnection currently connected to any network?
        /// </summary>
        public bool IsConnected;

        public NetConnection(ushort id, Network netManager)
        {
            NetManager = netManager;

            NUID = null;
            IsConnected = false;
            ConnectionId = id;
        }

        /// <summary>
        /// Disconnects / kicks the client from network.
        /// </summary>
        public void Disconnect()
        {
            IsConnected = false;
            NUID = null;
        }
    }
}