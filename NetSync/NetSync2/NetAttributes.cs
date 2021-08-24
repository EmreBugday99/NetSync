using System;

namespace NetSync2
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RPC : Attribute
    {
        public Target Network;
        public RpcType Type;

        public RPC(Target target, RpcType type)
        {
            Network = target;
            Type = type;
        }
    }
}