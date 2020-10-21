using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSync.Client;
using NetSync.Server;
using NetSync.Transport.AsyncTcp;
using System.Threading;

namespace NetSync.UnitTests
{
    public class NetworkObjectTestClass
    {
        public bool Created;

        public NetworkObjectTestClass()
        {
            Created = true;
        }
    }

    [TestClass]
    public class PacketHandlerTests
    {
        [TestMethod]
        public void SendPacket_BothSidesReceives()
        {
            bool serverReceivedPacket = false;
            bool clientReceivedPacket = false;

            #region Server Initialization

            //server
            AsyncTcp serverTransport = new AsyncTcp();
            NetworkServer server = new NetworkServer(2403, 5, 4095, serverTransport);
            server.StartServer();

            server.RegisterHandler(0, (connection, packet) =>
            {
                string msg = packet.ReadString();
                int msg2 = packet.ReadInteger();
                float msg3 = packet.ReadFloat();

                if (msg == "I Am Alive!" && msg2 == 12345 && msg3 == 123.456f)
                    serverReceivedPacket = true;
            });

            #endregion Server Initialization

            Thread.Sleep(1000);

            #region Clients Initialization

            //client 1
            AsyncTcp client1Transport = new AsyncTcp();
            NetworkClient client1 = new NetworkClient("127.0.0.1", 2403, 4095, client1Transport);

            //client 2
            AsyncTcp client2Transport = new AsyncTcp();
            NetworkClient client2 = new NetworkClient("127.0.0.1", 2403, 4095, client2Transport);

            //client3
            AsyncTcp client3Transport = new AsyncTcp();
            NetworkClient client3 = new NetworkClient("127.0.0.1", 2403, 4095, client3Transport);

            client1.StartClient();
            client2.StartClient();
            client3.StartClient();

            client1.RegisterHandler(0, packet =>
            {
                string msg = packet.ReadString();
                int msg2 = packet.ReadInteger();
                float msg3 = packet.ReadFloat();

                if (msg == "I Am Alive!" && msg2 == 12345 && msg3 == 123.456f)
                    clientReceivedPacket = true;
            });

            #endregion Clients Initialization

            Thread.Sleep(1000);

            #region Server Sending Data

            Packet serverPacket = new Packet();
            serverPacket.WriteString("I Am Alive!");
            serverPacket.WriteInteger(12345);
            serverPacket.WriteFloat(123.456f);

            server.NetworkSendEveryone(0, serverPacket);

            #endregion Server Sending Data

            #region Client Sending Data

            Packet clientPacket = new Packet();
            clientPacket.WriteString("I Am Alive!");
            clientPacket.WriteInteger(12345);
            clientPacket.WriteFloat(123.456f);

            client1.NetworkSend(0, clientPacket);

            #endregion Client Sending Data

            Thread.Sleep(2000);

            Assert.IsTrue(serverReceivedPacket && clientReceivedPacket);
        }
    }
}