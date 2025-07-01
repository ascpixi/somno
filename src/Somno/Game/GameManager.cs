using Somno.Game.Entities;
using Somno.Game.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Somno.Game.SourceEngine;

namespace Somno.Game
{
    internal static class GameManager
    {
        const int MaxEntities = 4096; // see https://developer.valvesoftware.com/wiki/Entity_limit
        const int ServerTickRate = 64;
        static readonly TimeSpan updateDelay = TimeSpan.FromMilliseconds((1f / (ServerTickRate * 1.5f)) * 1000);
        static DateTime lastUpdated = DateTime.MinValue;

        static readonly PlayerEntity[] playerBuffer = new PlayerEntity[64];
        static LocalPlayerEntity currentPlayer;
        static PlantedC4 c4;
        static uint cachedC4EntIdx = uint.MaxValue;
        static uint dwClientState;

        /// <summary>
        /// The absolute path to the game executable file.
        /// </summary>
        public static string? ProcessPath { get; private set; }

        /// <summary>
        /// The absolute path to the directory that contains installed maps.
        /// </summary>
        public static string? MapDirectoryPath { get; private set; }

        /// <summary>
        /// Whether the game is currently running.
        /// </summary>
        public static bool Running { get; private set; }

        /// <summary>
        /// The current player. This value is only valid if <see cref="PlayerSpawned"/> is <see langword="true"/>.
        /// </summary>
        public static LocalPlayerEntity CurrentPlayer => currentPlayer;

        /// <summary>
        /// All connected players. This value is only valid if <see cref="PlayerSpawned"/> is <see langword="true"/>.
        /// </summary>
        public static CollectionAccessor<PlayerEntity> Players { get; private set; } = new();
        
        /// <summary>
        /// Whether the player has physically spawned in the game world. This value
        /// is <see langword="false"/> when the player is, for example, in the team selection screen.
        /// </summary>
        public static bool PlayerSpawned { get; private set; }

        /// <summary>
        /// Whether the bomb has been planted, and is present in the world
        /// as a physical entity.
        /// </summary>
        public static bool BombPlanted { get; private set; }

        /// <summary>
        /// The physical entity, representing the bomb. This value is only valid
        /// if <see cref="BombPlanted"/> is <see langword="true"/>.
        /// </summary>
        public static PlantedC4 Bomb => c4;

        /// <summary>
        /// The connection state of the client.
        /// </summary>
        public static SignOnState ClientNetworkState { get; private set; }

        /// <summary>
        /// The "curtime" global variable.
        /// </summary>
        public static float CurrentTime { get; private set; }

        /// <summary>
        /// Whether the player is connected to a server.
        /// </summary>
        public static bool ConnectedToServer => ClientNetworkState == SignOnState.Full;
       
        /// <summary>
        /// Whether the map has fully initialized, and the player has physically
        /// spawned in the game world.
        /// </summary>
        public static bool Playing => Running && ConnectedToServer && PlayerSpawned;

        /// <summary>
        /// Represents the current map.
        /// </summary>
        public static Map? Map { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pid"></param>
        public static void ChangeProcessPID(int pid)
        {
            ProcessPath = ProcessQuery.GetPathByPID(pid);
            if (string.IsNullOrEmpty(ProcessPath)) {
                throw new Exception($"Could not get the executable path of PID {pid}.");
            }

            MapDirectoryPath = Path.Combine(
                Path.GetDirectoryName(ProcessPath)!,
                "csgo/maps"
            );
        }

        /// <summary>
        /// Reads the name of the map. This is a lazy operation, meaning each
        /// call means a read from the game's memory.
        /// </summary>
        public static string ReadMapName()
        {
            var addr = MemoryManager.Engine.Read<uint>(Offsets.dwClientState);

            if(addr == 0) {
                return string.Empty;
            }

            return MemoryManager.Global.ReadMemoryString(addr + Offsets.dwClientState_Map);
        }

        public static void Update()
        {
            if (Orchestrator.State != EngineState.Running) {
                Running = false;
                Map = null;
                dwClientState = 0;
                return;
            }

            Running = true;

            if (DateTime.UtcNow - lastUpdated < updateDelay) {
                return;
            }

            if(dwClientState == 0) {
                dwClientState = MemoryManager.Engine.Read<uint>(Offsets.dwClientState);
            }

            ClientNetworkState = MemoryManager.Global.Read<SignOnState>(dwClientState + Offsets.dwClientState_State);

            if (!ConnectedToServer) {
                Map = null;
                return;
            }

            PlayerSpawned = LocalPlayerEntity.TryFromMemoryPointer(
                MemoryManager.Client, Offsets.dwLocalPlayer,
                out currentPlayer
            );

            if(!PlayerSpawned) {
                Map = null;
                return;
            }

            if (Map == null) {
                string mapName = ReadMapName();
                Map = new Map(mapName);
            }

            // Read camera data
            Camera.ViewAngles = MemoryManager.Global.Read<Vector3>(dwClientState + Offsets.dwClientState_ViewAngles);
            MemoryManager.Client.ReadMemory(Offsets.dwViewMatrix, Camera.ViewMatrix);

            CurrentTime = MemoryManager.Engine.Read<float>(Offsets.dwGlobalVars + 0x10);

            uint grp = MemoryManager.Client.Read<uint>(Offsets.dwGameRulesProxy);
            if(grp != 0) {
                BombPlanted = MemoryManager.Global.Read<bool>(grp + Offsets.m_bBombPlanted);
            } else {
                Terminal.LogWarning("Couldn't find game rules proxy.");
            }

            int length = 0;
            for (uint i = 1; i < 64; i++) {
                bool success = PlayerEntity.TryFromMemoryPointer(
                    MemoryManager.Client,
                    Offsets.dwEntityList + (i * 0x10),
                    out var playerEntity
                );

                if (!success) {
                    continue;
                }

                playerBuffer[length] = playerEntity;
                length++;
            }

            ValidateEntities();

            Players = new(playerBuffer, length);

            Running = true;
            lastUpdated = DateTime.UtcNow;
        }
    
        static bool TryGetCachedC4()
        {
            if (cachedC4EntIdx == uint.MaxValue) {
                return false; // no cached C4 to read
            }

            uint entityAddr = MemoryManager.Client.Read<uint>(Offsets.dwEntityList + (cachedC4EntIdx * 0x10));
            if(entityAddr == 0) {
                return false;
            }

            var id = EntityTyping.GetClassID(entityAddr);

            if (id != ClassID.CPlantedC4) {
                return false;
            }
                
            c4 = PlantedC4.FromMemory(MemoryManager.Global, entityAddr);
            return true;
        }

        public static void ValidateEntities()
        {
            bool needBombRescan = false;

            if(BombPlanted && !TryGetCachedC4()) {
                needBombRescan = true;
            }

            uint i = 63;
            while(needBombRescan && i < MaxEntities) {
                i++;

                uint entityPtrAddr = Offsets.dwEntityList + (i * 0x10);
                uint entityAddr = MemoryManager.Client.Read<uint>(entityPtrAddr);
                if (entityAddr == 0) continue;

                var id = EntityTyping.GetClassID(entityAddr);
                if (needBombRescan && id == ClassID.CPlantedC4) {
                    c4 = PlantedC4.FromMemory(MemoryManager.Global, entityAddr);
                    cachedC4EntIdx = i;
                    needBombRescan = false;
                }
            }
        }
    }
}
