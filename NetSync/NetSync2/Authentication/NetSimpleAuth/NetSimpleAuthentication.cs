namespace NetSync2.Authentication.NetSimpleAuth
{
    public class NetSimpleAuthentication : AuthBase
    {
        public readonly Network NetManager;

        public NetSimpleAuthentication(Network network)
        {
            NetManager = network;
        }

        public override void Client_RequestAuthenticationFromServer(NetClient client)
        {
        }
    }
}