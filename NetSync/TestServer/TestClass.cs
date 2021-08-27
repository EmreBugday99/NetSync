using NetSync2;
using NetSync2.Transport.NetUdp;

namespace TestServer
{
    public class TestClass
    {
        private struct TestMessage : ISyncMessage
        {
            public string Name;

            public void Serialize(ref Packet packet)
            {
                packet.WriteInteger(12);
            }

            public void DeSerialize(ref Packet packet)
            {
                Name = packet.ReadString();
            }
        }

        public void DoSomething()
        {
            NetUdpManager ad = new NetUdpManager("127.0.0.1", 2405, 2406);
            Network net = new Network(ad, 2046);
        }
    }
}