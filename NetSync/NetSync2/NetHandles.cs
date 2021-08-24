namespace NetSync2
{
    public enum Target : byte
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