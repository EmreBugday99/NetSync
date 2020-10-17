using System;

namespace QueueTest
{
    public class Program
    {
        private static Server _server;
        private static Client _client;

        private static void Main(string[] args)
        {
            Start();
        }

        private static void Start()
        {
            string input = Console.ReadLine();

            if (input == "server")
                _server = new Server();
            else
            {
                for (int i = 0; i < 100; i++)
                {
                    _client = new Client();
                }
            }
        }
    }
}