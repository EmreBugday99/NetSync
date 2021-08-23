namespace NetSync2.Server
{
    public class NetServer
    {
        public readonly Network NetManager;

        public readonly ushort ConnectionLimit;
        internal NetConnection[] Connections;

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