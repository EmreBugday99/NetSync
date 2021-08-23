using System;

namespace NetSync2
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RPC : Attribute
    {
        public TargetType NetworkType;
        public RpcType Type;

        public RPC(TargetType targetType, RpcType rpcType)
        {
            NetworkType = targetType;
            Type = rpcType;
        }
    }
}