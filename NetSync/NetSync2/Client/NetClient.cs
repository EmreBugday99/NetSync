namespace NetSync2.Client
{
    public class NetClient
    {
        public readonly Network NetManager;
        public NetConnection LocalConnection;

        public NetClient(Network netManager)
        {
            NetManager = netManager;
        }

        public void InvokeRpc(Network.RpcHandle rpcHandle)
        {
            Packet packet = new Packet();
            rpcHandle.Invoke(ref packet);

            string rpcName = rpcHandle.Method.Name;
            RpcHandle handle = NetManager.RpcDictionary[rpcName.GetStableHashCode()];

            packet.InsertInteger(0, handle.RpcHash);
            NetManager.Transport.SendRpc(handle, ref packet);
        }

        public void InvokeRpc(string rpcName)
        {
            Packet packet = new Packet();

            RpcHandle handle = NetManager.GetHandleWithHash(rpcName.GetStableHashCode());

            handle.Handle.Invoke(ref packet);

            packet.InsertInteger(0, handle.RpcHash);
            NetManager.Transport.SendRpc(handle, ref packet);
        }
    }
}