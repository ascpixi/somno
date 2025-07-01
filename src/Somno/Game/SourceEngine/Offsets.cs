namespace Somno.Game.SourceEngine;

// The names of these constants are equal to the Source engine global/member variable names.
#pragma warning disable IDE1006

internal static class Offsets
{
    public const int dwEntityList = 0x4DFFF7C;

    public const int dwLocalPlayer = 0xDEA98C;
    public const int dwViewMatrix = 0x4DF0DC4;
    public const int dwClientState = 0x59F19C;

    public const int dwClientState_Map = 0x28C;
    public const int dwClientState_State = 0x108;
    public const int dwClientState_ViewAngles = 0x4D90;

    public const int dwGameRulesProxy = 0x532F4E4;
    public const int dwGlobalVars = 0x59EE60;

    public const int m_iItemDefinitionIndex = 0x2FBA;

    public const int m_vecOrigin = 0x138;
    public const int m_vecViewOffset = 0x108;
    public const int m_vecVelocity = 0x114;
    public const int m_iTeamNum = 0xF4;
    public const int m_iHealth = 0x100;
    public const int m_bDormant = 0xED;
    public const int m_hActiveWeapon = 0x2F08;
    public const int m_fFlags = 0x104;

    public const int m_bBombPlanted = 0x9A5;

    public const int m_bBombTicking = 0x2990;
    public const int m_nBombSite = 0x2994;
    public const int m_flC4Blow = 0x29a0;

    public const int m_bIsScoped = 0x9974;

    public const int m_aimPunchAngle = 0x303C;
    public const int m_iShotsFired = 0x103E0;

    public const int m_pStudioHdr = 0x2950;
    public const int m_dwBoneMatrix = 0x26A8;
}
