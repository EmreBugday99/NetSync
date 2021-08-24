using NetSync2.Client;
using NetSync2.Runtime;
using NetSync2.Server;
using NetSync2.Transport;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NetSync2
{
    public class Network
    {
        public readonly ushort PacketSize;

        public NetServer NetworkServer;
        public NetClient NetworkClient;

        internal TransportBase Transport;

        public delegate void RpcHandle(ref Packet packet);

        public Dictionary<int, NetSync2.RpcHandle> RpcDictionary;
        public Dictionary<int, NetClass> NetworkedClasses;

        public delegate void NetworkError(string errorMsg);

        public event NetworkError OnNetworkError;

        public Network(TransportBase transport, ushort packetSize)
        {
            Transport = transport;
            PacketSize = packetSize;
            NetworkServer = null;
            NetworkClient = null;
            RpcDictionary = new Dictionary<int, NetSync2.RpcHandle>();
        }

        public NetServer CreateServer(ushort connectionLimit)
        {
            NetworkServer = new NetServer(connectionLimit, this);
            Transport.StartServer(NetworkServer);
            return NetworkServer;
        }

        public NetServer GetServer()
        {
            return NetworkServer;
        }

        public NetClient CreateClient()
        {
            NetworkClient = new NetClient(this);
            Transport.StartClient(NetworkClient);

            return NetworkClient;
        }

        public NetClient GetClient()
        {
            return NetworkClient;
        }

        public void RegisterRpc(RpcHandle rpcHandle)
        {
            //TODO: Change return statements as error events

            RPC rpc = rpcHandle.Method.GetCustomAttribute<RPC>();
            if (rpc == null)
                return;
            if (rpcHandle.Method.DeclaringType == null)
                return;

            int rpcHash = rpcHandle.Method.Name.GetStableHashCode();

            if (RpcDictionary.ContainsKey(rpcHash))
            {
                Console.WriteLine("RPC Already registered!");
                return;
            }

            NetSync2.RpcHandle handle = new NetSync2.RpcHandle
            {
                Handle = rpcHandle,
                RpcHash = rpcHash,
                Target = rpc.RpcTarget,
            };

            RpcDictionary.Add(handle.RpcHash, handle);
        }

        public void RemoveRpc(RpcHandle rpcHandle)
        {
            RpcDictionary.Remove(rpcHandle.Method.Name.GetHashCode());
        }

        public NetSync2.RpcHandle GetHandleWithHash(int hash)
        {
            return RpcDictionary[hash];
        }

        public void NetOnReceive(ref Packet packet, Target rpc)
        {
            int rpcHash = packet.ReadInteger();

            NetSync2.RpcHandle handle = RpcDictionary[rpcHash];

            if (handle.Target == rpc)
                handle.Handle.Invoke(ref packet);
        }

        public virtual void InvokeNetworkError(string errorMsg)
        {
            OnNetworkError?.Invoke(errorMsg);
        }
    }
}