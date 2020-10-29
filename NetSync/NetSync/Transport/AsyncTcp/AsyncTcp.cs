using NetSync.Client;
using NetSync.Server;
using System;
using System.Net;
using System.Net.Sockets;

namespace NetSync.Transport.AsyncTcp
{
    public class AsyncTcp : TransportBase
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

                //Framing
                ushort totalPacketSizeRead = 0;
                while (totalPacketSizeRead < byteLength)
                {
                    //Each time we iterate totalPacketSizeRead will also increase based on the packet length
                    ushort packetLength = BitConverter.ToUInt16(data, totalPacketSizeRead);

                    if (totalPacketSizeRead > byteLength)
                    {
                        OnClientErrorDetected("Framing Bug! I am still looking for the cause of this issue. During massively high amount data transfer the framing algorithm I setup seems to broke up!");
                        continue;
                    }

                    byte[] framedData = new byte[packetLength];
                    Array.Copy(data, totalPacketSizeRead, framedData, 0, packetLength);

                    //We can increase the read length as we copied the data to the array.
                    totalPacketSizeRead += packetLength;

                    Packet packet = new Packet(framedData);

                    //We just do this to increase the read position in the buffer.
                    ushort packetSize = packet.ReadUnsignedShort();
                    byte channel = packet.ReadByte();
                    byte packetId = packet.ReadByte();

                    PacketHeader packetHeader = new PacketHeader(channel, packetId);
                    OnClientDataReceive(packet, packetHeader);
                }
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

                //Inserting the length of the packet
                //Increasing by two to include uShort's byte length as well.
                packet.InsertUnsignedShort(0, (ushort)(packet.GetByteArray().Length + 2));

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
            _tcpClient?.Close();
            _receiveBuffer = null;
            _netStream = null;
            _tcpClient = null;
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

                _serverConnections[connection.ConnectionId] = new ServerConnection(tcpClient, _bufferSize, this, connection);
                connection.UAI = tcpClient.Client.RemoteEndPoint.ToString();
                OnServerConnect(connection);
                break;
            }
        }

        public override void ServerDisconnect(Connection connection)
        {
            if (_serverConnections[connection.ConnectionId] == null) return;
            _serverConnections[connection.ConnectionId].Disconnect();
            _serverConnections[connection.ConnectionId] = null;
            OnServerDisconnect(connection);
        }

        public override void ServerSend(Connection connection, Packet packet, PacketHeader packetHeader)
        {
            packet.InsertByte(0, packetHeader.Channel);
            packet.InsertByte(1, packetHeader.PacketId);

            //Inserting the length of the packet
            //Increasing by two to include uShort's byte length as well.
            packet.InsertUnsignedShort(0, (ushort)(packet.GetByteArray().Length + 2));

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
            private readonly int _bufferSize;
            private byte[] _receiveBuffer;

            private readonly Connection _connection;
            private readonly AsyncTcp _asyncTcp;

            internal ServerConnection(TcpClient tcpClient, int bufferSize, AsyncTcp asyncTcp, Connection connection)
            {
                _asyncTcp = asyncTcp;
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

                    //Framing
                    ushort totalPacketSizeRead = 0;
                    while (totalPacketSizeRead < byteLength)
                    {
                        //Each time we iterate totalPacketSizeRead will also increase based on the packet length
                        ushort packetLength = BitConverter.ToUInt16(data, totalPacketSizeRead);

                        //TODO: I need to fix this mess!
                        //When there is massive(I mean truly massive. Like SHIT LOAD OF MASSIVE) amount of data being received; framing algorithm seems to fuck up.
                        //It thinks that there is more bytes to read than the current byteLength we have. This is fucked up...
                        //I don't know the reason behind this. I might even write an entirely different framing algorithm at this point...
                        //Oh god please help me found wtf is wrong with this bs...
                        //But for some stupid reason this if statement seems to fix the issue WITH NO DATA LOSS... WTF!?
                        if (totalPacketSizeRead > byteLength)
                            continue;

                        byte[] framedData = new byte[packetLength];
                        Array.Copy(data, totalPacketSizeRead, framedData, 0, packetLength);

                        //We can increase the read length as we copied the data to the array.
                        totalPacketSizeRead += packetLength;

                        Packet packet = new Packet(framedData);

                        //We just do this to increase the read position in the buffer.
                        ushort packetSize = packet.ReadUnsignedShort();
                        byte channel = packet.ReadByte();
                        byte packetId = packet.ReadByte();

                        PacketHeader packetHeader = new PacketHeader(channel, packetId);
                        _asyncTcp.OnServerDataReceive(_connection, packet, packetHeader);
                    }

                    //Packet packetReceived = new Packet(data);
                    //byte channel = packetReceived.ReadByte();
                    //byte packetId = packetReceived.ReadByte();
                    //PacketHeader packetHeader = new PacketHeader(channel, packetId);
                    //_asyncTcp.OnServerDataReceive(_connection, packetReceived, packetHeader);
                }
                catch (Exception exception)
                {
                    _connection.Disconnect();
                    _asyncTcp.OnServerErrorDetected($"Error while receiving data from client [{_connection.ConnectionId}] : " + exception);
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
                    _asyncTcp.OnServerErrorDetected($"Error while sending data to client: {exception}");
                }
            }

            internal void Disconnect()
            {
                _tcpClient?.Client?.Disconnect(true);
                _tcpClient?.Close();
                _receiveBuffer = null;
                _tcpClient = null;
                _netStream = null;

                lock (_connection.ConnectionLock)
                {
                    _connection.IsConnected = false;
                    _connection.HandshakeCompleted = false;
                }
            }
        }

        #endregion Server
    }
}