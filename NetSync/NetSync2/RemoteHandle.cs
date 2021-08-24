namespace NetSync2
{
    public struct RemoteHandle
    {
        public Network.RpcHandle RpcHandle;
        public int RpcHash;

        public Target Target;
        public RpcType Type;
    }
}