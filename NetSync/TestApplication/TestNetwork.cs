using NetSync2;
using System;

namespace TestApplication
{
    public class TestNetwork
    {
        public void DoSomething()
        {
            Network network = new Network();

            network.ConnectClient("127.0.0.1", 1234);
            network.BindRpc(TestRpc);

            Packet packet = new Packet();
            packet.WriteInteger("TestRpc".GetHashCode());

            Packet packet2 = new Packet(packet.GetByteArray());

            network.NetOnReceive(packet2, NetType.NetClient);
        }

        [RPC(NetType.NetClient)]
        public void TestRpc(Packet packet)
        {
            Console.WriteLine("Test RPC invoked;");
        }
    }
}