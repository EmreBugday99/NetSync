using NetSync2.Client;
using NetSync2.Server;
using System.Collections.Generic;
using System.Reflection;

namespace NetSync2
{
    public class Network
    {
        internal NetServer NetworkServer;
        internal NetClient NetworkClient;

        public delegate void RpcHandle(Packet packet);
        private Dictionary<int, RemoteHandle> _rpcDictionary;

        public Network()
        {
            NetworkServer = null;
            NetworkClient = null;
            _rpcDictionary = new Dictionary<int, RemoteHandle>();
        }

        public NetServer CreateServer(ushort connectionLimit)
        {
            NetworkServer = new NetServer(connectionLimit, this);
            return NetworkServer;
        }

        public NetServer GetServer()
        {
            return NetworkServer;
        }

        public NetClient ConnectClient(string ip, int port)
        {
            NetworkClient = new NetClient(ip, port, this);
            return NetworkClient;
        }

        public NetClient GetClient()
        {
            return NetworkClient;
        }

        public void BindRpc(RpcHandle rpcHandle)
        {
            RPC rpc = rpcHandle.Method.GetCustomAttribute<RPC>();
            if (rpc == null)
                return;

            RemoteHandle handle = new RemoteHandle
            {
                RpcHandle = rpcHandle,
                RpcHash = rpcHandle.Method.Name.GetHashCode(),
                NetworkType = rpc.NetworkType
            };

            _rpcDictionary.Add(handle.RpcHash, handle);
        }

        public void UnbindRpc(RpcHandle rpcHandle)
        {
            _rpcDictionary.Remove(rpcHandle.Method.Name.GetHashCode());
        }

        public void InvokeRpc(RpcHandle rpcHandle)
        {}

        public void InvokeRpc(string rpcName)
        { }

        public void NetOnReceive(Packet packet, NetType rpcType)
        {
            int rpcHash = packet.ReadInteger();

            RemoteHandle handle = _rpcDictionary[rpcHash];

            if(handle.NetworkType == rpcType)
                handle.RpcHandle.Invoke(packet);
        }
    }
}