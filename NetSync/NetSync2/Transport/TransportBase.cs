using NetSync2.Client;
using NetSync2.Server;

namespace NetSync2.Transport
{
    public abstract class TransportBase
    {
        public abstract void StartClient(NetClient client);
        public abstract void DisconnectClient(NetClient client);

        public abstract void StartServer(NetServer server);
        public abstract void ServerTerminateConnection(NetConnection connection);
        public abstract void ServerStop(NetServer server);

        public abstract void SendRpc(RemoteHandle handle, ref Packet packet);
    }
}