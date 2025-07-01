using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game.Entities
{
    // m_fFlags
    // https://github.com/ValveSoftware/source-sdk-2013/blob/master/mp/src/public/const.h#L147
    [Flags]
    internal enum PlayerFlags : uint
    {
        OnGround = 1u << 0,
        Crouching = 1u << 1,
        MidCrouching = 1u << 2,
        WaterJump = 1u << 3,
        ControllingTrain = 1u << 4,
        StandingInRain = 1u << 5,
        ThirdPersonFrozen = 1u << 6,
        ControllingAnotherEntity = 1u << 7,
        Client = 1u << 8,
        FakeClient = 1u << 9,
        InWater = 1u << 10,
        Fly = 1u << 11,
        Swim = 1u << 12,
        Conveyor = 1u << 13,
        NPC = 1u << 14,
        GodMode = 1u << 15,
        NoTarget = 1u << 16,
        AimTarget = 1u << 17,
        PartialGround = 1u << 18,
        StaticProp = 1u << 19,
        Graphed = 1u << 20,
        Grenade = 1u << 21,
        StepMovement = 1u << 22,
        DontTouch = 1u << 23,
        BaseVelocity = 1u << 24,
        WorldBrush = 1u << 25,
        ObjectVisibleByNPCs = 1u << 26,
        Disposed = 1u << 27, // originally called "KILLME"
        OnFire = 1u << 28,
        Dissolving = 1u << 29,
        TransitionToRagdoll = 1u << 30,
        UnblockableByPlayer = 1u << 31
    }
}
