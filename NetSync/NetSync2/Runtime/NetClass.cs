using System;
using System.Collections.Generic;
using System.Reflection;

namespace NetSync2.Runtime
{
    public class NetClass
    {
        public Network NetManager;
        public Dictionary<int, RpcHandle> OwnedRpcs;
        public readonly Object Owner;

        public NetClass(Network network, Object owner)
        {
            if (owner.GetType().IsClass == false)
            {
                NetManager.InvokeNetworkError("NetClass Owner can only be a class!");
                return;
            }

            OwnedRpcs = new Dictionary<int, RpcHandle>();
            NetManager = network;
            Owner = owner;

            if (NetManager.NetworkedClasses.TryAdd(owner.GetType().FullName.GetStableHashCode(), this))
            {
                NetManager.InvokeNetworkError($"[{Owner.GetType().FullName}] already cached in NetworkedClasses");
            }
        }

        public void InitializeNetClass()
        {
            MethodInfo[] methods = Owner.GetType().GetMethods();
            foreach (MethodInfo method in methods)
            {
                RPC rpc = method.GetCustomAttribute<RPC>();
                if (rpc == null)
                {
                    NetManager.InvokeNetworkError($"{Owner.GetType().FullName} doesn't have any RPC attributed methods!");
                    return;
                }

                RpcHandle handle = new RpcHandle
                {
                    Handle = (Network.RpcHandle)Delegate.CreateDelegate(typeof(Network.RpcHandle), method),
                    RpcHash = method.Name.GetStableHashCode(),
                    Target = rpc.RpcTarget
                };

                if (OwnedRpcs.ContainsKey(handle.RpcHash) == false)
                {
                    NetManager.InvokeNetworkError($"[{Owner.GetType().FullName}.{method.Name}] already cached!");
                    return;
                }

                OwnedRpcs.Add(handle.RpcHash, handle);
            }
        }

        public void InvokeRpc(Network.RpcHandle rpcHandle, NetConnection connection = null)
        {
            int rpcHash = rpcHandle.Method.Name.GetStableHashCode();
            RpcHandle handle = OwnedRpcs[rpcHash];

            if (handle.Target == Target.NetClient && connection == null)
            {
                NetManager.InvokeNetworkError($"[{rpcHandle.Method.Name}] is targeted for Clients but provided Connection is null!");
                return;
            }

            Packet packet = new Packet();
            packet.Connection = connection;

            handle.Handle.Invoke(ref packet);

            packet.InsertInteger(0, rpcHash);
            NetManager.Transport.SendRpc(handle, ref packet);
        }

        public void InvokeRpc(string rpcName, NetConnection connection = null)
        {
            Packet packet = new Packet();
            packet.Connection = connection;

            RpcHandle handle = NetManager.GetHandleWithHash(rpcName.GetStableHashCode());
            if (handle.Target == Target.NetClient && connection == null)
            {
                NetManager.InvokeNetworkError($"[{handle.Handle.Method.Name}] is targeted for Clients but provided Connection is null!");
                return;
            }

            handle.Handle.Invoke(ref packet);

            packet.InsertInteger(0, handle.RpcHash);
            NetManager.Transport.SendRpc(handle, ref packet);
        }
    }
}