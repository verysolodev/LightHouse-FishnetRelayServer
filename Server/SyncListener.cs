using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LightHouseRelayServer
{
    public class SyncListener : INetEventListener
    {
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
            Console.WriteLine($"Sync connection request from {request.RemoteEndPoint}");
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine($"Sync network error: {endPoint} - {socketError}");
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            try
            {
                if (LightHouseRelay.AllPlayers.TryGetValue(peer, out NetPlayer player))
                {
                    byte messageType = reader.GetByte();

                    switch (messageType)
                    {
                        case 0: // Create Host
                            int maxPlayers = reader.GetInt();
                            LightHouseRelay.MakeRoom(player, maxPlayers);
                            break;

                        case 1: // Chat message
                            string chatMessage = reader.GetString();
                            Console.WriteLine($"Chat from {player.PlayerName}: {chatMessage}");
                            break;

                        case 2: // Player info
                            string playerName = reader.GetString();
                            string deviceInfo = reader.GetString();
                            player.PlayerName = playerName;
                            player.DeviceType = deviceInfo;
                            Console.WriteLine($"Player registered: {playerName} ({deviceInfo})");
                            break;

                        case 3: // Join Room
                            string roomKey = reader.GetString();
                            LightHouseRelay.WaitingPlayers.Add(player);
                            
                            player.writer.Reset();
                            player.writer.Put((byte)1);
                            player.writer.Put(player.JoinRoom(roomKey));
                            
                            peer.Send(player.writer , DeliveryMethod.ReliableOrdered , 0);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing sync message: {ex}");
            }
            finally
            {
                reader.Recycle();
            }
        }
        

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine($"Sync client connected: {peer.Address}");
            
            var player = new NetPlayer { SyncPeer = peer };
            LightHouseRelay.AllPlayers.TryAdd(peer, player);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine($"Sync client disconnected: {peer.Address} - {disconnectInfo.Reason}");
            
            if (LightHouseRelay.AllPlayers.TryRemove(peer, out NetPlayer player))
            {
                player.Disconnect();
            }
        }
    }
}