namespace NetSync.Client
{
    internal struct ClientQueueHandle
    {
        internal Packet ReceivedPacket;
        internal ClientHandle Handle;

        internal ClientQueueHandle(Packet receivedPacket, ClientHandle handle)
        {
            ReceivedPacket = receivedPacket;
            Handle = handle;
        }
    }
}