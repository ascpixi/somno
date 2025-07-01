using System;

namespace Somno.Game.Entities;

/// <summary>
/// Represents additional entity information attributes for a
/// memory capture to include.
/// </summary>
[Flags]
internal enum EntityCaptureFlags
{
    /// <summary>
    /// Capture necessary information. This flag is always set.
    /// </summary>
    Default = 0,
    Weapon = (1 << 0),
    ScopedIn = (1 << 1),
    Recoil = (1 << 2),
    ShotsFired = (1 << 3)
}
