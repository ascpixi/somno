using Somno.Game.SourceEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game.Entities
{
    /// <summary>
    /// Represents a planted bomb in the game world.
    /// </summary>
    internal struct PlantedC4
    {
        /// <summary>
        /// The position of the bomb, in world-space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Whether the explosion countdown has been started, and the bomb
        /// is fully planted, ready for detonation.
        /// </summary>
        public bool BombTicking;

        /// <summary>
        /// The bomb-site index the bomb has been planted at. Usually, the
        /// index follows the alphabetical ordering of the bomb sites, if such
        /// ordering exists.
        /// </summary>
        public int BombSite;

        /// <summary>
        /// The time, from the start of the game client, of the C4 detonation.
        /// This does not include the 1-second delay between the bomb being
        /// completely undefusable and its actual explosion.
        /// </summary>
        public float C4Blow;

        /// <summary>
        /// The amount of time, in seconds, till the bomb represented by this
        /// structure detonates. Similarly to <see cref="C4Blow"/>, this does not
        /// include the 1-second delay between detonation and explosion.
        /// </summary>
        public float TimeToDetonation => C4Blow - GameManager.CurrentTime;

        internal static bool TryFromMemoryPointer(MemoryManager memory, uint offset, out PlantedC4 entity)
        {
            uint addr = memory.Read<uint>(offset);

            if (addr > 0) {
                entity = FromMemory(MemoryManager.Global, addr);
                return true;
            }

            entity = default;
            return false;
        }

        internal static PlantedC4 FromMemory(MemoryManager memory, uint offset)
        {
            var p = new PlantedC4();
            p.Position = memory.Read<Vector3>(offset + Offsets.m_vecOrigin);
            p.BombTicking = memory.Read<bool>(offset + Offsets.m_bBombTicking);
            p.BombSite = memory.Read<int>(offset + Offsets.m_nBombSite);
            p.C4Blow = memory.Read<float>(offset + Offsets.m_flC4Blow);
            return p;
        }
    }
}
