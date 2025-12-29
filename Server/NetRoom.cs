using System;
using System.Collections.Generic;

namespace LightHouseRelayServer
{
    public class NetRoom
    {
        private static readonly Random _random = new Random();
        private static readonly string _chars = "abcdefghijklmnopqrstuvwxyz0123456789";

        public static string GenerateKey()
        {
            char[] key = new char[6];
            for (int i = 0; i < 6; i++)
            {
                key[i] = _chars[_random.Next(_chars.Length)];
            }
            return new string(key);
        }

        public int MaxPlayer { get; set; } = 10;
        public string Key { get; set; }
        public NetPlayer Host { get; set; }
        public List<NetPlayer> Clients { get; set; } = new List<NetPlayer>();

        public void AddPlayer(NetPlayer player)
        {
            if (Clients.Count < MaxPlayer)
            {
                Clients.Add(player);
                player.NetRoom = this;
                Console.WriteLine($"Player {player.PlayerName} added to room {Key}");
            }
        }

        public void RemovePlayer(NetPlayer player)
        {
            Clients.Remove(player);
            Console.WriteLine($"Player {player.PlayerName} removed from room {Key}");
            
            if (Clients.Count == 0 && Host == null)
            {
                ClearRoom();
            }
        }

        public void ClearRoom()
        {
            LightHouseRelay.AllRooms.TryRemove(Key, out _);
            Console.WriteLine($"Room {Key} cleared and removed");
        }
    }
}