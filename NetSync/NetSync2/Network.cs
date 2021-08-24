using NetSync2.Client;
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
        private Dictionary<int, RemoteHandle> _rpcDictionary;

        public delegate void NetworkError(string errorMsg);

        public event NetworkError OnNetworkError;

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

        public NetClient CreateClient()
        {
            NetworkClient = new NetClient(this);
            Transport.StartClient(NetworkClient);
            InvokeRpc("NetSync_ClientHandshakeWithServer");
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

            if (_rpcDictionary.ContainsKey(rpcHash))
            {
                Console.WriteLine("RPC Already registered!");
                return;
            }

            RemoteHandle handle = new RemoteHandle
            {
                RpcHandle = rpcHandle,
                RpcHash = rpcHash,
                Target = rpc.Network,
                Type = rpc.Type
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
            packet.Connection = target;
            rpcHandle.Invoke(ref packet);

            string rpcName = rpcHandle.Method.Name;
            RemoteHandle handle = _rpcDictionary[rpcName.GetStableHashCode()];

            packet.InsertInteger(0, handle.RpcHash);
            Transport.SendRpc(handle, ref packet);
        }

        public void InvokeRpc(string rpcName, NetConnection target = null)
        {
            Packet packet = new Packet();
            packet.Connection = target;

            RemoteHandle handle = GetHandleWithHash(rpcName.GetStableHashCode());

            handle.RpcHandle.Invoke(ref packet);

            packet.InsertInteger(0, handle.RpcHash);
            Transport.SendRpc(handle, ref packet);
        }

        public void NetOnReceive(ref Packet packet, Target rpc)
        {
            int rpcHash = packet.ReadInteger();

            RemoteHandle handle = _rpcDictionary[rpcHash];

            if (handle.Target == rpc)
                handle.RpcHandle.Invoke(ref packet);
        }

        public virtual void InvokeNetworkError(string errorMsg)
        {
            OnNetworkError?.Invoke(errorMsg);
        }
    }
}