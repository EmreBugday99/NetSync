using System.Collections.Generic;

namespace NetSync2.Server
{
    public class NetServer
    {
        public readonly Network NetManager;

        public readonly ushort ConnectionLimit;
        internal readonly NetConnection[] Connections;
        public Dictionary<string, NetConnection> ActiveConnections;

        public NetServer(ushort connectionLimit, Network netManager)
        {
            NetManager = netManager;
            ConnectionLimit = connectionLimit;

            Connections = new NetConnection[connectionLimit];
            for (ushort i = 0; i < Connections.Length; i++)
            {
                Connections[i] = new NetConnection(i, netManager);
            }
        }
    }
}