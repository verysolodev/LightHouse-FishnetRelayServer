using System.Collections.Concurrent;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LightHouseRelayServer
{
    public abstract class NetLight
    {
        public NetPlayer Player { get; set; }
        public virtual void TakePackage(byte[] data, DeliveryMethod deliveryMethod, int dataSpanLength) 
        {
            Console.WriteLine("Warning: NetLight.TakePackage called from base class");
        }
    }

    public class HostLight : NetLight
    {
        public override void TakePackage(byte[] packet, DeliveryMethod deliveryMethod, int dataSpanLength)
        {
            // Host receives data from clients - relay to game host logic
            Console.WriteLine($"Host received {packet.Length} bytes from client");
        }
    }

    public class ClientLight : NetLight
    {
        private NetManager _hostConnector;
        private NetPeer _hostPeer;
        private readonly ConcurrentQueue<(byte[], DeliveryMethod)> _clientPacketQueue = new();
        private readonly ConcurrentQueue<(byte[], DeliveryMethod)> _hostPacketQueue = new();
        private Thread _pollingThread;
        private bool _isRunning;

        public void StartConnection()
        {
            if (Player?.NetRoom?.Host?.SyncPeer == null)
            {
                Console.WriteLine("Cannot connect: Host information missing");
                return;
            }

            string hostAddress = Player.NetRoom.Host.SyncPeer.Address.ToString();
            int port = 7777; // Default game port

            Console.WriteLine($"Client {Player.PlayerName} connecting to {hostAddress}:{port}");

            var netListener = new EventBasedNetListener();
            _hostConnector = new NetManager(netListener);

            netListener.PeerConnectedEvent += peer =>
            {
                _hostPeer = peer;
                _isRunning = true;
                Console.WriteLine($"Client {Player.PlayerName} connected to host ooof");
                
            };
            
            netListener.ConnectionRequestEvent += peer =>
            {
                peer.Accept();
            };

            netListener.PeerDisconnectedEvent += (peer, info) =>
            {
                Console.WriteLine($"Client {Player.PlayerName} disconnected from host: {info.Reason}");
                _isRunning = false;
            };

            netListener.NetworkReceiveEvent += (peer, reader, channel, method) =>
            {
                
                // Get span instead of copying bytes when possible
                ReadOnlySpan<byte> data = reader.GetRemainingBytesSpan();
        
                // Process immediately without queuing when possible
                RelayPacketImmediately(peer, data, method);
                
                //Console.WriteLine($"Riciving paket from host");
                reader.Recycle();
            };

            _hostConnector.Start();
            _hostConnector.Connect(hostAddress, port, string.Empty);
            
            StartPolling();
            

            // Start transfer thread
            ThreadPool.QueueUserWorkItem(_ => TransferLoop());
        }

        private void RelayPacketImmediately(NetPeer peer, ReadOnlySpan<byte> data, DeliveryMethod deliveryMethod)
        {
            Player.RelayPeer?.Send(data, deliveryMethod);
        }

        private void StartPolling()
        {
            _pollingThread = new Thread(() =>
            {
                while (true)
                {
                    _hostConnector?.PollEvents();
                    Thread.Sleep(1);
                }
            })
            {
                Name = $"ClientLightPoll_{Player.PlayerName}",
                IsBackground = true
            };
            _pollingThread.Start();
        }

        private void TransferLoop()
        {
            while (true)
            {
                // Process client → host packets
                if (_hostPeer != null && _clientPacketQueue.TryDequeue(out var clientPacket))
                {
                    _hostPeer.Send(clientPacket.Item1, clientPacket.Item2);
                    //Console.WriteLine($"send paket from clint to host");
                }

                // Process host → client packets
                if (Player.RelayPeer != null && _hostPacketQueue.TryDequeue(out var hostPacket))
                {
                    Player.RelayPeer.Send(hostPacket.Item1, hostPacket.Item2);
                }

                Thread.Sleep(1);
            }
        }

        public override void TakePackage(byte[] packet, DeliveryMethod deliveryMethod, int dataSpanLength)
        {
            ReadOnlySpan<byte> data = packet.AsSpan(0, dataSpanLength);
            
            if (_hostPeer == null)
            {
                _clientPacketQueue.Enqueue((data.ToArray(), deliveryMethod));
            }
            else
            {
                _hostPeer.Send(data, deliveryMethod);
            }
            
        }

        public void Stop()
        {
            _isRunning = false;
            _hostConnector?.Stop();
        }
    }
}