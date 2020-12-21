using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSync.Client;
using NetSync.Server;
using NetSync.Transport.AsyncTcp;
using System.Threading;

namespace NetSync.UnitTests
{
    [TestClass]
    public class ConnectionTests
    {
        [TestMethod]
        public void CanConnect_MultipleClientsConnecting_ConnectionEstablishes()
        {
            #region Server Initialization

            //server
            AsyncTcp serverTransport = new AsyncTcp();
            NetworkServer server = new NetworkServer(2401, 5, 4095, serverTransport);
            server.StartServer();

            #endregion Server Initialization

            Thread.Sleep(1000);

            #region Clients Initialization

            //client 1
            AsyncTcp client1Transport = new AsyncTcp();
            NetworkClient client1 = new NetworkClient("127.0.0.1", 2401, 4095, client1Transport);

            //client 2
            AsyncTcp client2Transport = new AsyncTcp();
            NetworkClient client2 = new NetworkClient("127.0.0.1", 2401, 4095, client2Transport);

            //client3
            AsyncTcp client3Transport = new AsyncTcp();
            NetworkClient client3 = new NetworkClient("127.0.0.1", 2401, 4095, client3Transport);

            client1.StartClient();
            client2.StartClient();
            client3.StartClient();

            #endregion Clients Initialization

            Thread.Sleep(1000);

            Assert.IsTrue(client1.IsActive() && client2.IsActive() && client3.IsActive());
        }

        [TestMethod]
        public void CanDisconnect_DisconnectedWithoutCrashingServer()
        {
            #region Server Initialization

            //server
            AsyncTcp serverTransport = new AsyncTcp();
            NetworkServer server = new NetworkServer(2402, 5, 4095, serverTransport);
            server.StartServer();

            #endregion Server Initialization

            Thread.Sleep(1000);

            #region Clients Initialization

            //client 1
            AsyncTcp client1Transport = new AsyncTcp();
            NetworkClient client1 = new NetworkClient("127.0.0.1", 2402, 4095, client1Transport);

            //client 2
            AsyncTcp client2Transport = new AsyncTcp();
            NetworkClient client2 = new NetworkClient("127.0.0.1", 2402, 4095, client2Transport);

            //client3
            AsyncTcp client3Transport = new AsyncTcp();
            NetworkClient client3 = new NetworkClient("127.0.0.1", 2402, 4095, client3Transport);

            client1.StartClient();
            client2.StartClient();
            client3.StartClient();

            #endregion Clients Initialization

            Thread.Sleep(1000);

            client1.StopClient();

            Thread.Sleep(1000);

            Assert.IsTrue(!client1.IsActive() && server.IsServerActive());
        }

        [TestMethod]
        public void CanStartServer_StartedWithoutError()
        {
            AsyncTcp transport = new AsyncTcp();
            NetworkServer server = new NetworkServer(0, 2, 4095, transport);
            server.StartServer();

            Thread.Sleep(1000);
            Assert.IsTrue(server.IsServerActive());
        }

        [TestMethod]
        public void CanStopServer_ServerStoppedWithoutError()
        {
            AsyncTcp transport = new AsyncTcp();
            NetworkServer server = new NetworkServer(0, 2, 4095, transport);
            server.StartServer();

            Thread.Sleep(1000);
            server.StopServer();
            Thread.Sleep(1000);

            Assert.IsFalse(server.IsServerActive());
        }
    }
}