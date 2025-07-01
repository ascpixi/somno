namespace Somno.Game.Entities;

/// <summary>
/// Represents the team of a player.
/// </summary>
internal enum TeamType : int
{
    Unassigned = 0,
    Spectators = 1,
    Terrorists = 2,
    CounterTerrorists = 3,

    /// <summary>
    /// The player's team is undefined. This value should only be used
    /// in serialization contexts.
    /// </summary>
    Undefined = byte.MaxValue
}
