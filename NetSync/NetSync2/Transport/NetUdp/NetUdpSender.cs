using System.Net;
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

        internal void SendMessage(ref Packet packet, IPEndPoint receiver)
        {
            SenderSocket.SendTo(packet.GetByteArray(), receiver);
        }
    }
}