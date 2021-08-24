﻿using NetSync2;
using NetSync2.Transport.NetUdp;

namespace TestClient
{
    public class TestNetwork
    {
        public Network Net;

        public void DoSomething()
        {
            NetUdpManager udpTransport = new NetUdpManager("127.0.0.1", 2445, 2446);
            Net = new Network(udpTransport, 2048);

            Net.CreateClient();
        }
    }
}