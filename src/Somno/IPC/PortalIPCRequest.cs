namespace Somno.IPC
{
    internal enum PortalIPCRequest : byte
    {
#if USE_ROOT_PORTAL
        Handshake = 0xCC,
        ReadProcessMemory = 0xD1
#else
        Handshake = 0xAA,
        ReadProcessMemory = 0xB1,
        Terminate = 0xBF
#endif
    }
}
