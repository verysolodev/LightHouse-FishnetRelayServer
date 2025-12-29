using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib;

namespace LightHouseRelayServer
{
    class Program
    {
        private static Thread _networkThread;
        private static LightHouseRelay _relayServer;
        private static bool _isRunning = true;

        static void Main(string[] args)
        {
            Console.WriteLine("=== LightHouse Relay Server Starting ===");
            
            // Setup console event handlers
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _isRunning = false;
            };

            try
            {
                _relayServer = new LightHouseRelay();
                _relayServer.StartServer();

                Console.WriteLine("Relay Server started successfully!");
                Console.WriteLine($"Relay Port: {_relayServer.RelayPort}");
                Console.WriteLine($"Sync Port: {_relayServer.SyncPort}");
                Console.WriteLine("Press Ctrl+C to stop the server...");

                // Main server loop
                _networkThread = new Thread(NetworkLoop)
                {
                    Name = "LowLatencyRelay",
                    Priority = ThreadPriority.Highest
                };
                _networkThread.Start();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex}");
            }
        }
        
        
        private static void NetworkLoop()
        {
            while (_isRunning)
            {
                _relayServer.PollEvents();
                Thread.Sleep(5); // Minimal sleep for maximum responsiveness
                
            }
            _relayServer?.Stop();
            Console.WriteLine("Relay Server stopped.");
            
        }
    }

    public class LightHouseRelay
    {
        public int RelayPort { get; private set; } = 2027;
        public int SyncPort { get; private set; } = 2028;

        private NetManager _relayServer, _syncServer;
        
        public static ConcurrentDictionary<NetPeer, NetPlayer> AllPlayers = new();
        public static List<NetPlayer> WaitingPlayers = new();
        public static ConcurrentDictionary<string, NetRoom> AllRooms = new();

        public void StartServer()
        {
            Console.WriteLine("Initializing servers...");

            // Relay Server
            _relayServer = new NetManager(new RelayListener());
            if (!_relayServer.Start(RelayPort))
            {
                throw new Exception($"Failed to start Relay server on port {RelayPort}");
            }
            _relayServer.UpdateTime = 15;

            // Sync Server
            _syncServer = new NetManager(new SyncListener());
            if (!_syncServer.Start(SyncPort))
            {
                throw new Exception($"Failed to start Sync server on port {SyncPort}");
            }
            _syncServer.UpdateTime = 15;

            Console.WriteLine($"Relay Server started on port {RelayPort}");
            Console.WriteLine($"Sync Server started on port {SyncPort}");
        }

        public void PollEvents()
        {
            _relayServer?.PollEvents();
            _syncServer?.PollEvents();
        }

        public void Stop()
        {
            Console.WriteLine("Stopping servers...");
            _relayServer?.Stop();
            _syncServer?.Stop();
            Console.WriteLine("All servers stopped.");
        }

        public static bool JoinPlayerToRoom(NetPeer relayPeer)
        {
            string targetAddress = relayPeer.Address.ToString();

            for (int i = 0; i < WaitingPlayers.Count; i++)
            {
                string playerAddress = WaitingPlayers[i].SyncPeer.Address.ToString();
                
                if (playerAddress == targetAddress)
                {
                    WaitingPlayers[i].MakeClientLight(relayPeer);
                    WaitingPlayers.RemoveAt(i);
                    return true;
                }
            }

            Console.WriteLine($"No waiting player found for relay peer: {targetAddress}");
            return false;
        }

        public static void MakeRoom(NetPlayer host, int maxPlayers)
        {
            var room = new NetRoom();
            var hostLight = new HostLight();
            
            host.NetLight = hostLight;
            host.NetRoom = room;
            hostLight.Player = host;

            string key;
            do
            {
                key = NetRoom.GenerateKey();
            } while (AllRooms.ContainsKey(key));

            room.Key = key;
            room.MaxPlayer = maxPlayers;
            room.Host = host;
            
            AllRooms.TryAdd(key, room);
            
            host.writer.Reset();
            host.writer.Put((byte)0);
            host.writer.Put(key);
            
            host.SyncPeer.Send(host.writer , DeliveryMethod.ReliableOrdered , 0);
            Console.WriteLine($"Room created: {key} by {host.PlayerName} (Max players: {maxPlayers})");
        }
    }
}