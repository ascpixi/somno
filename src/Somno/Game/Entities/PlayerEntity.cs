using Somno.Game.SourceEngine;
using Somno.PortalAgent;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game.Entities
{
    /// <summary>
    /// Represents a player in the game world.
    /// </summary>
    internal class PlayerEntity
    {
        public const float Width = 32;
        public const float Height = 72;
        public const float HeightCrouching = 54;
        public const float EyeLevel = 64.093811f;
        public const float EyeLevelCrouching = 46.076218f;

        /// <summary>
        /// The team the player is on.
        /// </summary>
        public TeamType Team;

        /// <summary>
        /// The position of the player, in world-space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The health of the player, in HP.
        /// </summary>
        public int Health;

        /// <summary>
        /// Whether the entity representing the player is dormant.
        /// When an entity is not dormant, it is not active, and its information
        /// may be inaccurate or undefined.
        /// </summary>
        public bool Dormant;

        /// <summary>
        /// A bit-field representing general boolean states of the player.
        /// </summary>
        public PlayerFlags Flags;

        /// <summary>
        /// The set of hitboxes for this player entity.
        /// </summary>
        public MStudioBBox[] Hitboxes = Array.Empty<MStudioBBox>();

        /// <summary>
        /// The matrices for each bone of the player's model.
        /// </summary>
        public Matrix3x4[] BoneMatrices = Array.Empty<Matrix3x4>();

        /// <summary>
        /// Whether the player is currently crouching.
        /// </summary>
        public bool Crouching => (Flags & PlayerFlags.Crouching) != 0;

        /// <summary>
        /// Dereferences the pointer at the given <paramref name="offset"/> if it is not
        /// <see langword="null"/>, and returns player entity information at the
        /// dereferenced address.
        /// </summary>
        /// <param name="memory">The memory manager to use.</param>
        /// <param name="offset">The offset from the memory manager's base address.</param>
        /// <param name="entity">The variable to save the player information to.</param>
        /// <returns><see langword="true"/> if the pointer is not <see langword="null"/> and has been dereferenced successfully - <see langword="false"/> otherwise.</returns>
        internal static bool TryFromMemoryPointer(MemoryManager memory, uint offset, [MaybeNullWhen(false)] out PlayerEntity entity)
        {
            uint addr = memory.Read<uint>(offset);

            if (addr > 0) {
                entity = FromMemory(MemoryManager.Global, addr);
                return true;
            }

            entity = null!;
            return false;
        }

        /// <summary>
        /// Reads the player at the given offset.
        /// </summary>
        /// <param name="memory">The memory manager to use.</param>
        /// <param name="offset">The offset from the memory manager base address.</param>
        /// <returns>The player entity data at the given <paramref name="offset"/> from the memory manager base address.</returns>
        internal static PlayerEntity FromMemory(MemoryManager memory, uint offset)
        {
            var p = new PlayerEntity();
            p.Position = memory.Read<Vector3>(offset + Offsets.m_vecOrigin);
            p.Team = memory.Read<TeamType>(offset + Offsets.m_iTeamNum);
            p.Health = memory.Read<int>(offset + Offsets.m_iHealth);
            p.Dormant = memory.Read<int>(offset + Offsets.m_bDormant) == 0;
            p.Flags = memory.Read<PlayerFlags>(offset + Offsets.m_fFlags);

            p.ReadBoneData(memory, offset);
            p.ReadHitboxData(memory, offset);

            return p;
        }

        unsafe void ReadBoneData(MemoryManager memory, uint offset)
        {
            var mg = MemoryManager.Global;

            var pBoneMatrix = memory.Read<uint>(offset + Offsets.m_dwBoneMatrix);
            BoneMatrices = new Matrix3x4[MStudioBBox.MaxStudioBones];
            for (uint i = 0; i < MStudioBBox.MaxStudioBones; i++) {
                var matrix = mg.Read<Matrix3x4>(pBoneMatrix + (i * (uint)sizeof(Matrix3x4)));
                BoneMatrices[i] = matrix;
            }
        }

        unsafe void ReadHitboxData(MemoryManager memory, uint offset)
        {
            var mg = MemoryManager.Global;

            var ppStudioHdr = memory.Read<uint>(offset + Offsets.m_pStudioHdr);
            var pstudioHdr = mg.Read<uint>(ppStudioHdr);
            var studioHdr = mg.Read<StudioHeader>(pstudioHdr);

            var pHitboxSet = pstudioHdr + studioHdr.HitboxSetIndex;
            var hitboxSet = mg.Read<MStudioHitboxSet>(pHitboxSet);

            Hitboxes = new MStudioBBox[hitboxSet.NumHitboxes];
            for (uint i = 0; i < hitboxSet.NumHitboxes; i++) {
                Hitboxes[i] = mg.Read<MStudioBBox>(pHitboxSet + hitboxSet.HitboxIndex + (i * (uint)sizeof(MStudioBBox)));
            }
        }
    }
}
