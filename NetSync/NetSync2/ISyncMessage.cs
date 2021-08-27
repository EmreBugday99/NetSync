namespace NetSync2
{
    public interface ISyncMessage
    {
        public void Serialize(ref Packet packet);
        public void DeSerialize(ref Packet packet);
    }
}