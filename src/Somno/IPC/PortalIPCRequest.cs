namespace Somno.IPC
{
    internal enum PortalIPCRequest : byte
    {
        Handshake = 0xAA,
        ReadProcessMemory = 0xB1,
        Terminate = 0xB2
    }
}
