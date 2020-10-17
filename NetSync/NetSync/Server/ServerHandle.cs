namespace NetSync.Server
{
    internal struct ServerHandle
    {
        internal readonly NetworkServer.MessageHandle Handler;
        internal readonly bool IsQueued;

        internal ServerHandle(NetworkServer.MessageHandle handler, bool isQueued)
        {
            Handler = handler;
            IsQueued = isQueued;
        }
    }
}