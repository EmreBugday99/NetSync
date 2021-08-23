using NetSync2;
using NetSync2.Transport.NetUdp;
using System;

namespace TestClient
{
    public class TestNetwork
    {
        public Network Net;

        public void DoSomething()
        {
            NetUdpManager udpTransport = new NetUdpManager(2045, 2046);
            Net = new Network(udpTransport, 2048);

            Net.RegisterRpc(TestRpc);
            Net.CreateClient("127.0.0.1", 2045);
            Net.InvokeRpc(TestRpc);
        }

        [RPC(TargetType.NetServer, RpcType.Send)]
        private void TestRpc(ref Packet packet)
        {
            packet.WriteString("What's UP mother fucker?");
        }
    }
}