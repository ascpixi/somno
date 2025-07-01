using Somno.Game.SourceEngine;
using System.Numerics;

namespace Somno.Game.Entities;

/// <summary>
/// Represents a local player in the game world. A local player is
/// a player that the client sends information about to the server.
/// </summary>
internal struct LocalPlayerEntity
{
    /// <summary>
    /// The team the player is on.
    /// </summary>
    public TeamType Team;

    /// <summary>
    /// The position of the player, in world-space.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// The offset from the player's <see cref="Position"/> to their
    /// eye position.
    /// </summary>
    public Vector3 ViewOffset;

    /// <summary>
    /// The health of the player, in HP.
    /// </summary>
    public int Health;

    /// <summary>
    /// The velocity of the player.
    /// </summary>
    public Vector3 Velocity;

    /// <summary>
    /// Whether the entity representing the player is dormant.
    /// When an entity is not dormant, it is not active, and its information
    /// may be inaccurate or undefined.
    /// </summary>
    public bool Dormant;

    /// <summary>
    /// The ID of the weapon the player is holding.
    /// </summary>
    public WeaponType Weapon;

    /// <summary>
    /// Whether the player is scoped in.
    /// </summary>
    public bool IsScoped;

    /// <summary>
    /// The deviation angle, in the yaw and pitch axis, of the recoil.
    /// Also known as the "aim punch angle".
    /// </summary>
    public Vector2 RecoilAngle;

    /// <summary>
    /// The amount of shots in the series that is being fired by the player.
    /// </summary>
    public int ShotsFired;

    /// <summary>
    /// The position of the player's eyes.
    /// </summary>
    public Vector3 EyePosition => Position + ViewOffset;

    /// <summary>
    /// Dereferences the pointer at the given <paramref name="offset"/> if it is not
    /// <see langword="null"/>, and returns local player entity information at the
    /// dereferenced address.
    /// </summary>
    /// <param name="memory">The memory manager to use.</param>
    /// <param name="offset">The offset from the memory manager's base address.</param>
    /// <param name="entity">The variable to save the player information to.</param>
    /// <returns><see langword="true"/> if the pointer is not <see langword="null"/> and has been dereferenced successfully - <see langword="false"/> otherwise.</returns>
    internal static bool TryFromMemoryPointer(MemoryManager memory, uint offset, out LocalPlayerEntity entity)
    {
        uint addr = memory.Read<uint>(offset);

        if (addr > 0) {
            entity = FromMemory(MemoryManager.Global, addr);
            return true;
        }

        entity = default;
        return false;
    }

    /// <summary>
    /// Reads the local player information at the given offset.
    /// </summary>
    /// <param name="memory">The memory manager to use.</param>
    /// <param name="offset">The offset from the memory manager base address.</param>
    /// <returns>The player entity data at the given <paramref name="offset"/> from the memory manager base address.</returns>
    internal static LocalPlayerEntity FromMemory(MemoryManager memory, uint offset)
    {
        var p = new LocalPlayerEntity();
        p.Position = memory.Read<Vector3>(offset + Offsets.m_vecOrigin);
        p.ViewOffset = memory.Read<Vector3>(offset + Offsets.m_vecViewOffset);
        p.Team = memory.Read<TeamType>(offset + Offsets.m_iTeamNum);
        p.Health = memory.Read<int>(offset + Offsets.m_iHealth);
        p.Velocity = memory.Read<Vector3>(offset + Offsets.m_vecVelocity);
        p.Dormant = memory.Read<int>(offset + Offsets.m_bDormant) == 0;
        p.IsScoped = memory.Read<bool>(offset + Offsets.m_bIsScoped);
        p.RecoilAngle = memory.Read<Vector2>(offset + Offsets.m_aimPunchAngle);
        p.ShotsFired = memory.Read<int>(offset + Offsets.m_iShotsFired);

        unchecked {
            uint curWeaponAddress = memory.Read<uint>(offset + Offsets.m_hActiveWeapon) & 0xFFF;
            uint iBase = MemoryManager.Client.Read<uint>(Offsets.dwEntityList + ((curWeaponAddress - 1) * 0x10));
            p.Weapon = MemoryManager.Global.Read<WeaponType>(iBase + Offsets.m_iItemDefinitionIndex);
        }

        return p;
    }
}
