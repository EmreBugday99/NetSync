using NetSync2;
using NetSync2.Transport.NetUdp;

namespace TestServer
{
    public class TestNetwork
    {
        public Network Net;

        public void DoSomething()
        {
            NetUdpManager udpTransport = new NetUdpManager("127.0.0.1", 2445, 2446);
            Net = new Network(udpTransport, 2048);

            Net.CreateServer(5);

            while (true)
            {
                udpTransport.ExecuteRpcBuffer();
            }
        }
    }
}