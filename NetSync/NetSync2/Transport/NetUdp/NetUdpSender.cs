using NetSync2.Client;
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
        }

        internal void InvokeRpc(RemoteHandle handle, ref Packet packet)
        {
            if (handle.Target == TargetType.NetServer)
                SenderSocket.SendTo(packet.GetByteArray(), _network.NetworkClient.ServerEndPoint);
            else if (handle.Target == TargetType.NetClient)
                SenderSocket.SendTo(packet.GetByteArray(), packet.TargetConnection.EndPoint);
        }
    }
}