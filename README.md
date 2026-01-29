# LightHouse
### A Fishnet Relay Solution

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Fishnet](https://img.shields.io/badge/Built%20with-Fishnet-0078D4)](https://fish-networking.gitbook.io/docs/)
[![LiteNetLib](https://img.shields.io/badge/Powered%20by-LiteNetLib-00A4EF)](https://github.com/RevenantX/LiteNetLib)

LightHouse is a high-performance packet relay solution built on top of Fishnet networking framework using LiteNetLib and C#/.NET. It enables efficient client-to-client communication through a centralized relay server.

## 📋 Features

- **🏗️ Robust Architecture** - Built on Fishnet's proven networking framework
- **⚡ High Performance** - Leverages LiteNetLib for efficient UDP communication
- **🔄 Packet Relay** - Seamless client-to-client packet forwarding
- **🔧 Easy Integration** - Simple API for quick setup and deployment
- **📱 Multi-Platform** - Supports Windows, Linux, and cross-platform deployments

## 🚀 Getting Started

### Prerequisites
- Unity 2021.3 LTS or newer
- FishNet Networking (latest version)
- LightHouse package imported to your project

### Installation Steps

1. **Import LightHouse Prefab**
   - Drag the `LightHouse.prefab` into your scene
   - Position it appropriately in your scene hierarchy

2. **Configure LightHouse Client**
   - Select the LightHouse GameObject
   - In the Inspector, locate the `LightHouseClient` component
   - Configure the following properties:
     ```
     Relay Port: 7777      # Must match server relay port
     Sync Port: 7778       # Must match server sync port
     ```

3. **Configure FishNet Transport**
   - Select your NetworkManager GameObject
   - Set Transport Type to **Tugboat**
   - Configure Tugboat settings:
     ```
     Port: 7777            # Must match LightHouse Relay Port
     ```

4. **Import Lantern UI**
   - Drag the `LanternWin.prefab` into your scene
   - This provides the connection interface

5. **Connect to Server**
   - Enter the server IP address in the Lantern UI
   - Click the **"Connect"** button
   - Check console for confirmation:
     ```
     [LightHouse] Connected to sync port successfully
     ```

6. **Create or Join Room**
   - **Option A: Create Room (Host)**
     1. Click **"Create Room"** button
     2. Note the generated room key from console
     3. Share this key with other players
   
   - **Option B: Join Room (Client)**
     1. Enter existing room key
     2. Click **"Join Room"** button

> **Note**: FishNet connection only establishes after room creation/joining. The initial connection is to LightHouse's sync port only.

### 📋 Configuration Reference

| Component | Property | Default Value | Description |
|-----------|----------|---------------|-------------|
| LightHouseClient | RelayPort | 7777 | UDP port for packet relay |
| LightHouseClient | SyncPort | 7778 | UDP port for room synchronization |
| Tugboat Transport | Port | 7777 | Must match RelayPort |

### 🐛 Troubleshooting
-Issue: "Failed to connect to sync port"

Verify server is running

Check firewall settings (ports 7777-7778 UDP/TCP)

Confirm IP address is correct

-Issue: "FishNet not connecting after room join"

Ensure Tugboat port matches RelayPort (7777)

Check room key is valid

Verify all players have same LightHouse version

-Issue: "Cannot create/join room"

Server sync service may be offline

Check console for specific error messages
