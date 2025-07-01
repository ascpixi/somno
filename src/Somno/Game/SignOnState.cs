namespace Somno.Game;

internal enum SignOnState : uint
{
    /// <summary>
    /// No state yet; about to connect.
    /// </summary>
    None = 0,

    /// <summary>
    /// Client challenging server; all OOB packets.
    /// </summary>
    Challenge = 1,

    /// <summary>
    /// Client is connected to server; net channels ready.
    /// </summary>
    Connected = 2,

    /// <summary>
    /// The client has recevied server information and string tables.
    /// </summary>
    New = 3,

    /// <summary>
    /// The client has received sign-on buffers.
    /// </summary>
    Prespawn = 4,

    /// <summary>
    /// The client is ready to receive entity packets.
    /// </summary>
    Spawn = 5,

    /// <summary>
    /// The client is fully connected.
    /// </summary>
    Full = 6,

    /// <summary>
    /// The server is changing the level (map).
    /// </summary>
    ChangeLevel = 7
}
