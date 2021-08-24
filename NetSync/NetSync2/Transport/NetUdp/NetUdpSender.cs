using System;
using System.Net.Sockets;

namespace NetSync2.Transport.NetUdp
{
    internal class NetUdpSender
    {
        internal Socket SenderSocket;

        private NetUdpManager _netUdp;
        private Network _network;

        public NetUdpSender(NetUdpManager netUdp)
        {
            _netUdp = netUdp;
            _network = netUdp.NetManager;

            SenderSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //SenderSocket.Connect(_network.NetworkClient.ServerEndPoint);
        }

        internal void InvokeRpc(RemoteHandle handle, ref Packet packet)
        {
            // We don't want server sending to server (it self).
            if (handle.Target == Target.NetServer && _network.NetworkServer == null)
                SenderSocket.SendTo(packet.GetByteArray(), _netUdp.ServerEndPoint);
            // Clients can't send to another client directly.
            else if (handle.Target == Target.NetClient && _network.NetworkClient == null)
                SenderSocket.SendTo(packet.GetByteArray(), packet.Connection.EndPoint);
            else
                _network.InvokeNetworkError("Failed to send RPC!");
        }
    }
}