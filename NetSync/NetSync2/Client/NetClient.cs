using System;
using NetSync2.Server;

namespace NetSync2.Client
{
    public class NetClient
    {
        public readonly Network NetManager;
        public NetConnection LocalConnection;

        public NetClient(Network netManager)
        {
            NetManager = netManager;
        }
    }
}