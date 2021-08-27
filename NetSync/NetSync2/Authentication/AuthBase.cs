namespace NetSync2.Authentication
{
    public abstract class AuthBase
    {
        public abstract void Client_RequestAuthenticationFromServer(NetClient client);
    }
}