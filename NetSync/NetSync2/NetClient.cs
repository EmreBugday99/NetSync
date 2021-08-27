namespace NetSync2
{
    public class NetClient
    {
        public readonly Network NetManager;
        public NetConnection LocalConnection;

        public NetClient(Network netManager)
        {
            NetManager = netManager;
        }

        public void SendMessage(ISyncMessage message)
        {
            Packet packet = new Packet();
            message.Serialize(ref packet);

            NetManager.Transport.SendMessageToServer(ref packet);
        }
    }
}