namespace NetSync.Server
{
    internal struct ServerQueueHandle
    {
        internal readonly Connection Connection;
        internal readonly Packet ReceivedPacket;
        internal ServerHandle Handle;

        internal ServerQueueHandle(Connection connection, Packet receivedPacket, ServerHandle handle)
        {
            Connection = connection;
            ReceivedPacket = receivedPacket;
            Handle = handle;
        }
    }
}