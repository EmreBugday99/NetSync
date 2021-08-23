using System;

namespace NetSync2
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RPC : Attribute
    {
        public NetType NetworkType;

        public RPC(NetType netType)
        {
            NetworkType = netType;
        }
    }
}