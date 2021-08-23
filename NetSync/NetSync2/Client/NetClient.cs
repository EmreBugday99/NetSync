using System.Net;

namespace NetSync2.Client
{
    public class NetClient
    {
        public readonly Network NetManager;
        public readonly IPEndPoint ServerEndPoint;
        public NetConnection Connection;

        public NetClient(string ip, int port, Network netManager)
        {
            ServerEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }
    }
}