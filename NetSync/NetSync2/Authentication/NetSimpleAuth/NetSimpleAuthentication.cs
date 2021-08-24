using NetSync2.Client;

namespace NetSync2.Authentication.NetSimpleAuth
{
    public class NetSimpleAuthentication : AuthBase
    {
        public readonly Network NetManager;

        public NetSimpleAuthentication(Network network)
        {
            NetManager = network;

            NetManager.RegisterRpc(NetSync_RequestAuthentication);
        }

        public override void Client_RequestAuthenticationFromServer(NetClient client)
        {
            NetManager.InvokeRpc(NetSync_RequestAuthentication);
        }

        [RPC(Target.NetServer, RpcType.Send)]
        private void NetSync_RequestAuthentication(ref Packet packet)
        {
        }

        [RPC(Target.NetServer, RpcType.Receive)]
        private void NetSync_AuthenticationReplyReceivedFromServer(ref Packet packet)
        {
            for (ushort i = 0; i < NetManager.NetworkServer.ConnectionLimit; i++)
            {
                NetConnection connection = NetManager.NetworkServer.Connections[i];

                if (connection.IsConnected == true)
                    continue;

                connection.ConnectionId = i;
                connection.EndPoint = packet.EndPoint;
                connection.IsConnected = true;

                Packet connectionPacket = new Packet();
            }
        }
    }
}