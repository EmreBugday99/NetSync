using System;

namespace NetSync2
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RPC : Attribute
    {
        public Target RpcTarget;

        public RPC(Target target)
        {
            RpcTarget = target;
        }
    }
}