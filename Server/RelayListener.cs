using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LightHouseRelayServer
{
    public class RelayListener : INetEventListener
    {
        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
            Console.WriteLine($"Relay connection request from {request.RemoteEndPoint}");
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine($"Relay network error: {endPoint} - {socketError}");
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            try
            {
                if (LightHouseRelay.AllPlayers.TryGetValue(peer, out NetPlayer player))
                {
                    ReadOnlySpan<byte> dataSpan = reader.GetRemainingBytesSpan();
                
                    // Use ArrayPool to avoid GC pressure
                    byte[] buffer = _arrayPool.Rent(dataSpan.Length);
                    try
                    {
                        dataSpan.CopyTo(buffer);
                        player.NetLight?.TakePackage(buffer, deliveryMethod, dataSpan.Length);
                    }
                    finally
                    {
                        _arrayPool.Return(buffer);
                    }
                }
            }
            finally
            {
                reader.Recycle();
            }
        
        }
        

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine($"Relay client connected: {peer.Address}");
            
            if (!LightHouseRelay.JoinPlayerToRoom(peer))
            {
                Console.WriteLine($"No matching player found for relay: {peer.Address}");
                peer.Disconnect();
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine($"Relay client disconnected: {peer.Address} - {disconnectInfo.Reason}");
            
            if (LightHouseRelay.AllPlayers.TryRemove(peer, out NetPlayer player))
            {
                player.RelayPeer = null;
            }
        }
    }
}