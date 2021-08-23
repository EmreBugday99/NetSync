namespace NetSync2.Client
{
    internal struct RemoteHandle
    {
        internal Network.RpcHandle RpcHandle;
        internal int RpcHash;

        internal NetType NetworkType;
    }
}