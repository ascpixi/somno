using Somno.PortalAgent;
using System;
using System.Threading;

namespace Somno.Game;

/// <summary>
/// Manages reads from the game's memory.
/// </summary>
internal class MemoryManager
{
    readonly Portal portalAgent;
    readonly ulong baseAddress;

    static MemoryManager? clientMod;
    static MemoryManager? engineMod;
    static MemoryManager? global;

    /// <summary>
    /// Represents the client.dll module memory manager.
    /// </summary>
    public static MemoryManager Client {
        get {
            if (clientMod == null)
                throw new InvalidOperationException("The 'client.dll' module memory manager isn't initialized.");
            
            return clientMod;
        }
    }

    /// <summary>
    /// Represents the engine.dll module memory manager.
    /// </summary>
    public static MemoryManager Engine {
        get {
            if (engineMod == null)
                throw new InvalidOperationException("The 'engine.dll' module memory manager isn't initialized.");
            
            return engineMod;
        }
    }

    /// <summary>
    /// Represents the global memory manager, which uses absolute addressing.
    /// </summary>
    public static MemoryManager Global {
        get {
            if (global == null)
                throw new InvalidOperationException("The global memory manager isn't initialized.");
            
            return global;
        }
    }

    /// <summary>
    /// Creates a new <see cref="MemoryManager"/> instance, using the given
    /// <paramref name="portalAgent"/> in order to read memory, and using the given
    /// <paramref name="baseAddress"/> for relative addressing.
    /// </summary>
    /// <param name="portalAgent">The portal agent to use for memory I/O.</param>
    /// <param name="baseAddress">The offset to add to each memory access.</param>
    public MemoryManager(Portal portalAgent, ulong baseAddress)
    {
        this.portalAgent = portalAgent;
        this.baseAddress = baseAddress;
    }

    /// <summary>
    /// Reinitializes memory managers to all required Source engine modules (DLLs).
    /// </summary>
    /// <param name="portalAgent">The portal agent to use for memory I/O.</param>
    public static void ReinitializeModules(Portal portalAgent)
    {
        int pid = portalAgent.TargetProcessPID;

        global = new MemoryManager(portalAgent, 0);

        Terminal.LogInfo("Initializing modules...");

        nint clientAddr, engineAddr;
        while (!ProcessQuery.TryGetModuleAddress(pid, "client.dll", out clientAddr)) {
            Thread.Sleep(16);
        }

        Terminal.LogInfo($"The client module has been loaded at 0x{clientAddr:X2}.");

        while (!ProcessQuery.TryGetModuleAddress(pid, "engine.dll", out engineAddr)) {
            Thread.Sleep(16);
        }

        Terminal.LogInfo($"The engine module has been loaded at 0x{engineAddr:X2}.");

        clientMod = new MemoryManager(portalAgent, (ulong)clientAddr);
        engineMod = new MemoryManager(portalAgent, (ulong)engineAddr);
    }

    public T Read<T>(ulong baseOffset) where T : unmanaged
    {
        if (!portalAgent.IsTargetRunning())
            throw new GameClosedException();

        return portalAgent.ReadProcessMemory<T>(baseAddress + baseOffset);
    }

    public void ReadMemory<T>(ulong baseOffset, T[] buffer) where T : unmanaged
    {
        if (!portalAgent.IsTargetRunning())
            throw new GameClosedException();

        portalAgent.ReadProcessMemory<T>(baseAddress + baseOffset, buffer);
    }

    public unsafe string ReadMemoryString(ulong baseOffset, byte bufferSize = 255)
    {
        if (!portalAgent.IsTargetRunning())
            throw new GameClosedException();

        sbyte[] chars = new sbyte[bufferSize];
        portalAgent.ReadProcessMemory(baseAddress + baseOffset, chars);

        fixed (sbyte* strPtr = chars) {
            return new string(strPtr);
        }
    }
}
