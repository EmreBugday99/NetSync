using NetSync2.Transport;

namespace NetSync2
{
    public class Network
    {
        public readonly ushort PacketSize;
        public NetServer NetworkServer;
        public NetClient NetworkClient;
        internal TransportBase Transport;

        public delegate void NetworkError(string errorMsg);
        public event NetworkError OnNetworkError;

        public Network(TransportBase transport, ushort packetSize)
        {
            Transport = transport;
            PacketSize = packetSize;
            NetworkServer = null;
            NetworkClient = null;
        }

        public NetServer CreateServer(ushort connectionLimit)
        {
            NetworkServer = new NetServer(connectionLimit, this);
            Transport.StartServer(NetworkServer);
            return NetworkServer;
        }

        public NetClient CreateClient()
        {
            NetworkClient = new NetClient(this);
            Transport.StartClient(NetworkClient);

            return NetworkClient;
        }

        public virtual void InvokeNetworkError(string errorMsg)
        {
            OnNetworkError?.Invoke(errorMsg);
        }
    }
}