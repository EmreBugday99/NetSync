namespace NetSync2
{
    public struct RemoteHandle
    {
        public Network.RpcHandle RpcHandle;
        public int RpcHash;

        public TargetType Target;
        public RpcType RpcType;

        public static int GetStableHashCode(string text)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in text)
                    hash = hash * 31 + c;
                return hash;
            }
        }
    }
}