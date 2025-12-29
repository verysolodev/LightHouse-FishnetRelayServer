using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LightHouseRelayServer
{
    public class NetPlayer
    {
        public string DeviceType { get; set; }
        public string PlayerName { get; set; }
        public NetPeer SyncPeer { get; set; }
        public NetPeer RelayPeer { get; set; }
        public NetRoom NetRoom { get; set; }
        public NetLight NetLight { get; set; }
        public NetDataWriter writer = new  NetDataWriter();

        public bool JoinRoom(string key)
        {
            if (LightHouseRelay.AllRooms.TryGetValue(key, out NetRoom room))
            {
                NetRoom = room;
                room.Clients.Add(this);
                Console.WriteLine($"Player {PlayerName} joined room {key}");
                return true;
            }
            else
            {
                Console.WriteLine($"Player {PlayerName} tried to join non-existent room: {key}");
            }
            return false;
        }

        public void MakeClientLight(NetPeer relayPeer)
        {
            RelayPeer = relayPeer;
            var clientLight = new ClientLight();
            NetLight = clientLight;
            clientLight.Player = this;

            LightHouseRelay.AllPlayers.TryAdd(relayPeer, this);
            
            Console.WriteLine($"Player {PlayerName} connecting to host...");
            
            // Start connection in background thread
            ThreadPool.QueueUserWorkItem(_ => 
            {
                try
                {
                    clientLight.StartConnection();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Connection failed for {PlayerName}: {ex}");
                }
            });
        }

        public void Disconnect()
        {
            if (NetRoom != null)
            {
                if (NetRoom.Host == this)
                {
                    Console.WriteLine($"Host {PlayerName} disconnected, clearing room {NetRoom.Key}");
                    NetRoom.ClearRoom();
                }
                else
                {
                    NetRoom.RemovePlayer(this);
                }
            }

            // Remove from global lists
            if (SyncPeer != null)
                LightHouseRelay.AllPlayers.TryRemove(SyncPeer, out _);
            if (RelayPeer != null)
                LightHouseRelay.AllPlayers.TryRemove(RelayPeer, out _);
                
            LightHouseRelay.WaitingPlayers.Remove(this);

            Console.WriteLine($"Player {PlayerName} fully disconnected");
        }
    }
}