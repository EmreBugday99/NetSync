﻿using NetSync2;
using System;
using NetSync2.Transport.NetUdp;

namespace TestApplication
{
    public class TestNetwork
    {
        public Network Net;
        public void DoSomething()
        {
            NetUdpManager udpTransport = new NetUdpManager(2045, 2046);
            Net = new Network(udpTransport, 2048);

            Net.RegisterRpc(TestRpc);
            Net.CreateServer(5);
        }

        [RPC(TargetType.NetServer, RpcType.Receive)]
        private void TestRpc(ref Packet packet)
        {
            string msg = packet.ReadString();

            Console.WriteLine(msg);
        }
    }
}