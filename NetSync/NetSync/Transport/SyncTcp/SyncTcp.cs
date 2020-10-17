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
                if (byteLength <= 0)
                {
                    ClientDisconnect();
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(_receiveBuffer, data, byteLength);

                _netStream.BeginRead(_receiveBuffer, 0, _bufferSize, ReceiveCallback, null);
                Packet packet = new Packet(data);

                byte channel = packet.ReadByte();
                byte packetId = packet.ReadByte();
                PacketHeader packetHeader = new PacketHeader(channel, packetId);
                OnClientDataReceive(packet, packetHeader);
            }
            catch (Exception exception)
            {
                ClientDisconnect();
                OnClientErrorDetected("Error while receiving data from server: " + exception);
            }
        }

        public override void ClientSendData(Packet packet, PacketHeader packetHeader)
        {
            try
            {
                packet.InsertByte(0, packetHeader.Channel);
                packet.InsertByte(1, packetHeader.PacketId);
                byte[] data = packet.GetByteArray();

                _netStream.BeginWrite(data, 0, data.Length, null, null);
            }
            catch (Exception exception)
            {
                ClientDisconnect();
                OnClientErrorDetected($"Error while sending data to server! {exception}");
            }
        }

        public override void ClientDisconnect()
        {
            _tcpClient.Client?.Disconnect(true);
            _tcpClient?.Close();
            _tcpClient = null;
            _netStream = null;
            _receiveBuffer = null;
            OnClientDisconnect();
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
                connection.UAI = tcpClient.Client.RemoteEndPoint.ToString();
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

        public override void ServerSend(Connection connection, Packet packet, PacketHeader packetHeader)
        {
            packet.InsertByte(0, packetHeader.Channel);
            packet.InsertByte(1, packetHeader.PacketId);
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
                    if (_netStream == null)
                    {
                        _connection.Disconnect();
                        return;
                    }

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
                    byte channel = packetReceived.ReadByte();
                    byte packetId = packetReceived.ReadByte();
                    PacketHeader packetHeader = new PacketHeader(channel, packetId);
                    _syncTcp.OnServerDataReceive(_connection, packetReceived, packetHeader);
                }
                catch (Exception exception)
                {
                    _connection.Disconnect();
                    _syncTcp.OnServerErrorDetected($"Error while receiving data from client [{_connection.ConnectionId}] : " + exception);
                }
            }

            internal void ServerSend(Packet packet)
            {
                try
                {

                    byte[] data = packet.GetByteArray();
                    _netStream.BeginWrite(data, 0, data.Length, null, null);
                }
                catch (Exception exception)
                {
                    _connection.Disconnect();
                    _syncTcp.OnServerErrorDetected(
                        $"Error while sending data to client: {exception}");
                }
            }

            internal void Disconnect()
            {
                _tcpClient?.Close();
                _receiveBuffer = null;
                _tcpClient = null;
                _netStream = null;
            }
        }

        #endregion Server
    }
}