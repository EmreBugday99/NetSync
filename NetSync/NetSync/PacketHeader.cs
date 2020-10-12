namespace NetSync
{
    public struct PacketHeader
    {
        public byte Channel;
        public byte PacketId;

        public PacketHeader(byte channel, byte packetId)
        {
            Channel = channel;
            PacketId = packetId;
        }
    }
}