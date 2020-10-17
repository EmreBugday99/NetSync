using NetSync;
using NetSync.Client;
using NetSync.Transport.SyncTcp;
using System;
using System.Threading;

namespace QueueTest
{
    public class Client
    {
        public NetworkClient TestClient;
        private SyncTcp _clientTransport;

        public Client()
        {
            string ip = "127.0.0.1";
            _clientTransport = new SyncTcp();
            TestClient = new NetworkClient(ip, 2405, 4095, _clientTransport);
            InitializeClient();

            TestClient.ConnectToServer();

            Thread queryUpdateThread = new Thread(QueryUpdateLoop);
            queryUpdateThread.Start();
        }

        private void InitializeClient()
        {
            TestClient.RegisterHandler(0, OnHelloReceive);
            TestClient.OnClientConnected += OnConnect;
        }

        private void OnConnect()
        {
            while (true)
            {
                Thread.Sleep(5);
                SendHello();
            }
        }

        private void OnHelloReceive(Packet packet)
        {
            string msg = packet.ReadString();
            string msg2 = packet.ReadString();

            Console.WriteLine(msg + " " + msg2);
        }

        private void QueryUpdateLoop()
        {
            while (true)
            {
                TestClient.ExecuteHandleQueue();
            }
        }

        private void SendHello()
        {
            Packet packet = new Packet();
            packet.WriteString("Hello From");
            packet.WriteString("Client!");

            TestClient.NetworkSend(1, packet);
        }
    }
}