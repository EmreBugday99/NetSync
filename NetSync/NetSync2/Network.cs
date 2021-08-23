using NetSync2.Client;
using NetSync2.Server;
using NetSync2.Transport;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NetSync2
{
    public class Network
    {
        public readonly ushort PacketSize;

        public NetServer NetworkServer;
        public NetClient NetworkClient;

        internal TransportBase Transport;

        public delegate void RpcHandle(ref Packet packet);
        private Dictionary<int, RemoteHandle> _rpcDictionary;

        public Network(TransportBase transport, ushort packetSize)
        {
            Transport = transport;
            PacketSize = packetSize;
            NetworkServer = null;
            NetworkClient = null;
            _rpcDictionary = new Dictionary<int, RemoteHandle>();
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

        public NetClient CreateClient(string ip, int port)
        {
            NetworkClient = new NetClient(ip, port, this);
            Transport.ConnectClient(NetworkClient);
            return NetworkClient;
        }

        public NetClient GetClient()
        {
            return NetworkClient;
        }

        public void RegisterRpc(RpcHandle rpcHandle)
        {
            RPC rpc = rpcHandle.Method.GetCustomAttribute<RPC>();
            if (rpc == null)
                return;

            if (rpcHandle.Method.DeclaringType == null)
                return;

            string rpcName = rpcHandle.Method.Name;

            Console.WriteLine(rpcName);
            RemoteHandle handle = new RemoteHandle
            {
                RpcHandle = rpcHandle,
                RpcHash = RemoteHandle.GetStableHashCode(rpcName),
                Target = rpc.NetworkType,
                RpcType = rpc.Type
            };

            _rpcDictionary.Add(handle.RpcHash, handle);
        }

        public void RemoveRpc(RpcHandle rpcHandle)
        {
            _rpcDictionary.Remove(rpcHandle.Method.Name.GetHashCode());
        }

        public RemoteHandle GetHandleWithHash(int hash)
        {
            return _rpcDictionary[hash];
        }

        public void InvokeRpc(RpcHandle rpcHandle, NetConnection target = null)
        {
            Packet packet = new Packet();
            packet.TargetConnection = target;
            rpcHandle.Invoke(ref packet);

            string rpcName = rpcHandle.Method.Name;
            RemoteHandle handle = _rpcDictionary[RemoteHandle.GetStableHashCode(rpcName)];

            packet.InsertInteger(0, handle.RpcHash);
            Transport.SendRpc(handle, ref packet);
        }

        public void InvokeRpc(string rpcName)
        { }

        public void NetOnReceive(ref Packet packet, TargetType rpcType)
        {
            int rpcHash = packet.ReadInteger();

            RemoteHandle handle = _rpcDictionary[rpcHash];

            if (handle.Target == rpcType)
                handle.RpcHandle.Invoke(ref packet);
        }
    }
}