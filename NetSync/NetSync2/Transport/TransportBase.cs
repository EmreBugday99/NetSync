namespace NetSync2.Transport
{
    public abstract class TransportBase
    {
        public abstract void StartClient(NetClient client);
        public abstract void DisconnectClient(NetClient client);
        public abstract void StartServer(NetServer server);
        public abstract void ServerTerminateConnection(NetConnection connection);
        public abstract void ServerStop(NetServer server);
        public abstract void SendMessageToServer(ref Packet packet);
        public abstract void SendMessageToClient(ref Packet packet, NetConnection connection);
    }
}