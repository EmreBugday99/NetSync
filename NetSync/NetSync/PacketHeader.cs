namespace NetSync
{
    public readonly struct PacketHeader
    {
        public readonly byte Channel;
        public readonly byte PacketId;

        public PacketHeader(byte channel, byte packetId)
        {
            Channel = channel;
            PacketId = packetId;
        }
    }
}