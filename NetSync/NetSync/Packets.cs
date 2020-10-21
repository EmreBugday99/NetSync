namespace NetSync
{
    internal enum Packets : byte
    {
        /// <summary>
        /// Initial handshake packet.
        /// </summary>
        Handshake = 0,
        /// <summary>
        /// Used for telling the client that server has accepted/authorized the handshake.
        /// </summary>
        SuccessfulHandshake,
        /// <summary>
        /// Used for syncing classes/ objects over network. AKA Network Objects.
        /// </summary>
        SyncNetworkObject
    }
}