using NetSync;
using NetSync.Server;
using NetSync.Transport.SyncTcp;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace QueueTest
{
    public class Server
    {
        public NetworkServer TestServer;
        private SyncTcp _serverTransport;

        public Server()
        {
            _serverTransport = new SyncTcp();
            TestServer = new NetworkServer(2405, 100, 4095, _serverTransport);
            InitializeServer();

            TestServer.Start();

            Thread queryUpdateThread = new Thread(QueryUpdateLoop);
            queryUpdateThread.Start();
        }

        private void InitializeServer()
        {
            TestServer.RegisterHandler(1, OnHelloReceive, 1, true);
            TestServer.OnServerConnected += OnClientConnected;
        }

        private void OnClientConnected(Connection connection)
        {
            //for (int i = 0; i < 5; i++)
            //{
            //    Thread.Sleep(5);
            //    SendHello();
            //}
        }

        private void OnHelloReceive(Connection connection, Packet packet)
        {
            string msg = packet.ReadString();
            string msg2 = packet.ReadString();

            Console.WriteLine(msg + " " + msg2);

            SendHello();
        }

        private void QueryUpdateLoop()
        {
            while (true)
            {
                TestServer.ExecuteHandleQueue();
            }
        }

        private void SendHello()
        {
            Packet packet = new Packet();
            packet.WriteString("Hello From");
            packet.WriteString("Server!");
            TestServer.NetworkSendEveryone(0, packet);
        }
    }
}