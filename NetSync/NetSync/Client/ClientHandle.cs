namespace NetSync.Client
{
    internal struct ClientHandle
    {
        internal NetworkClient.MessageHandle Handle;
        internal bool IsQueued;

        internal ClientHandle(NetworkClient.MessageHandle handle, bool isQueued)
        {
            Handle = handle;
            IsQueued = isQueued;
        }
    }
}