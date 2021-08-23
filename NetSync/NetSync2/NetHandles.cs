namespace NetSync2
{
    public enum TargetType : byte
    {
        NetServer = 0,
        NetClient
    }

    public enum RpcType : byte
    {
        Send = 0,
        Receive
    }
}