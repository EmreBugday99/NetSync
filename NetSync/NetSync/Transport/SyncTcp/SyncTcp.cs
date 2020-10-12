using NetSync.Client;
using NetSync.Server;
using System;
using System.Net;
using System.Net.Sockets;

namespace NetSync.Transport.SyncTcp
{
    public class SyncTcp : TransportBase
    {
        private TcpClient _tcpClient;
        private TcpListener _tcpListener;
        private NetworkStream _netStream;
        private ServerConnection[] _serverConnections;
        private byte[] _receiveBuffer;
        private int _bufferSize;

        private NetworkServer _networkServer;
        private NetworkClient _networkClient;

        #region Client

        public override void ClientConnect(NetworkClient client)
        {
            _bufferSize = client.DataBufferSize;
            _receiveBuffer = new byte[_bufferSize];
            _tcpClient = new TcpClient()
            {
                ReceiveBufferSize = _bufferSize,
                SendBufferSize = _bufferSize
            };
            _tcpClient.BeginConnect(client.ServerIp, client.ServerPort, ClientConnectCallback, null);

            _networkClient = client;
        }

        private void ClientConnectCallback(IAsyncResult result)
        {
            _tcpClient.EndConnect(result);

            if (_tcpClient.Connected == false)
                throw new Exception("Error while establishing connection with server!");

            _netStream = _tcpClient.GetStream();
            _netStream.BeginRead(_receiveBuffer, 0, _bufferSize, ReceiveCallback, null);
            OnClientConnect();
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int byteLength = _netStream.EndRead(result);
                Console.WriteLine("I received something: " + byteLength);
                if (byteLength <= 0)
                {
                    ClientDisconnect();
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(_receiveBuffer, data, byteLength);
                _netStream.BeginRead(_receiveBuffer, 0, _bufferSize, ReceiveCallback, null);
                Packet packet = new Packet(data);
                OnClientDataReceive(packet, 0);
            }
            catch (Exception exception)
            {
                ClientDisconnect();
                throw new Exception($"Error while receiving data from server! {exception}");
            }
        }

        public override void ClientDisconnect()
        {
            if (_tcpClient != null)
                _tcpClient.Close();

            _tcpClient = null;
            _netStream = null;
            _receiveBuffer = null;
            OnClientDisconnect();
        }

        public override void ClientSendData(Packet packet, byte channel)
        {
            try
            {
                if (_tcpClient == null) return;
                byte[] data = packet.GetByteArray();

                _netStream.BeginWrite(data, 0, data.Length, null, null);
            }
            catch
            {
                throw new Exception("Error while sending data to server!");
            }
        }

        #endregion Client

        #region Server

        public override void ServerStart(NetworkServer server)
        {
            _networkServer = server;
            _bufferSize = _networkServer.DataBufferSize;
            _receiveBuffer = new byte[_bufferSize];

            _tcpListener = new TcpListener(IPAddress.Any, _networkServer.ServerPort);
            _serverConnections = new ServerConnection[_networkServer.Connections.Length];

            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(ServerConnectionCallback, null);
            OnServerStart(_networkServer);
        }

        private void ServerConnectionCallback(IAsyncResult result)
        {
            TcpClient tcpClient = _tcpListener.EndAcceptTcpClient(result);
            _tcpListener.BeginAcceptTcpClient(ServerConnectionCallback, null);

            foreach (var connection in _networkServer.Connections)
            {
                if (connection.IsConnected) continue;

                ushort connectionId = connection.ConnectionId;
                _serverConnections[connectionId] = new ServerConnection(tcpClient, _bufferSize, this, connection);
                OnServerConnect(connection);
                break;
            }
        }

        public override void ServerDisconnect(Connection connection)
        {
            if (_serverConnections[connection.ConnectionId] == null) return;

            _serverConnections[connection.ConnectionId].Disconnect();
            connection.IsConnected = false;
            _serverConnections[connection.ConnectionId] = null;
            OnServerDisconnect(connection);
        }

        public override void ServerSend(Connection connection, Packet packet, byte channel)
        {
            _serverConnections[connection.ConnectionId].ServerSend(packet);
        }

        public override void ServerStop()
        {
            foreach (var connection in _networkServer.Connections)
            {
                connection.Disconnect();
            }

            _tcpListener.Stop();
            OnServerStop(_networkServer);
        }

        private class ServerConnection
        {
            private TcpClient _tcpClient;
            private NetworkStream _netStream;
            private int _bufferSize;
            private byte[] _receiveBuffer;

            private Connection _connection;
            private SyncTcp _syncTcp;

            internal ServerConnection(TcpClient tcpClient, int bufferSize, SyncTcp syncTcp, Connection connection)
            {
                _syncTcp = syncTcp;
                _connection = connection;
                _tcpClient = tcpClient;
                _bufferSize = bufferSize;
                _receiveBuffer = new byte[bufferSize];
                _tcpClient.ReceiveBufferSize = _bufferSize;
                _tcpClient.SendBufferSize = _bufferSize;
                _netStream = _tcpClient.GetStream();

                _netStream.BeginRead(_receiveBuffer, 0, _bufferSize, ServerReceiveCallback, null);
            }

            private void ServerReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = _netStream.EndRead(result);
                    if (byteLength <= 0)
                    {
                        _connection.Disconnect();
                        return;
                    }
                    byte[] data = new byte[byteLength];
                    Array.Copy(_receiveBuffer, data, byteLength);
                    _netStream.BeginRead(_receiveBuffer, 0, _bufferSize, ServerReceiveCallback, null);

                    Packet packetReceived = new Packet(data);
                    _syncTcp.OnServerDataReceive(_connection, packetReceived, 0);
                }
                catch
                {
                    _connection.Disconnect();
                }
            }

            internal void ServerSend(Packet packet)
            {
                try
                {
                    byte[] data = packet.GetByteArray();
                    _netStream.BeginWrite(data, 0, data.Length, null, null);
                }
                catch
                {
                    throw new Exception("Error while sending data to client!");
                }
            }

            internal void Disconnect()
            {
                _tcpClient.Close();
                _receiveBuffer = null;
                _tcpClient = null;
                _netStream = null;
            }
        }

        #endregion Server
    }
}