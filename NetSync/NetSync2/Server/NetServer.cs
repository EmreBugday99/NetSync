using System.Collections.Generic;

namespace NetSync2.Server
{
    public class NetServer
    {
        public readonly Network NetManager;

        public readonly ushort ConnectionLimit;
        internal readonly NetConnection[] Connections;
        public Dictionary<string, NetConnection> ActiveConnections;

        public NetServer(ushort connectionLimit, Network netManager)
        {
            NetManager = netManager;
            ConnectionLimit = connectionLimit;

            Connections = new NetConnection[connectionLimit];
            for (ushort i = 0; i < Connections.Length; i++)
            {
                Connections[i] = new NetConnection(i, netManager);
            }
        }

        public void InvokeRpc(Network.RpcHandle rpcHandle, NetConnection target)
        {
            Packet packet = new Packet();
            packet.Connection = target;
            rpcHandle.Invoke(ref packet);

            string rpcName = rpcHandle.Method.Name;
            RpcHandle handle = NetManager.RpcDictionary[rpcName.GetStableHashCode()];

            packet.InsertInteger(0, handle.RpcHash);
            NetManager.Transport.SendRpc(handle, ref packet);
        }

        public void InvokeRpc(string rpcName, NetConnection target)
        {
            Packet packet = new Packet();
            packet.Connection = target;

            RpcHandle handle = NetManager.GetHandleWithHash(rpcName.GetStableHashCode());

            handle.Handle.Invoke(ref packet);

            packet.InsertInteger(0, handle.RpcHash);
            NetManager.Transport.SendRpc(handle, ref packet);
        }
    }
}