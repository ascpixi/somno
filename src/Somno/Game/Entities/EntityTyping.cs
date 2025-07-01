namespace Somno.Game.Entities
{
    /// <summary>
    /// Provides various methods related to generic entity operations.
    /// </summary>
    internal static class EntityTyping
    {
        /// <summary>
        /// Gets the class ID of a entity that implements the IClientNetworkable interface.
        /// </summary>
        /// <param name="baseAddr">The global base address of the entity.</param>
        /// <returns>The class ID (type) of the entity.</returns>
        public static ClassID GetClassID(ulong baseAddr)
        {
            // Get the VTable from the IClientNetworkable object
            uint vtable = MemoryManager.Global.Read<uint>(baseAddr + 0x8);
            
            // Read the GetClientClass function from the VTable (index 3)
            uint getClientClass = MemoryManager.Global.Read<uint>(vtable + (2 * 0x4));
            
            // Read the pointer to the ClientClass struct from the mov eax, <imm32> instruction
            uint clientClass = MemoryManager.Global.Read<uint>(getClientClass + 1);
           
            // ...and then, finally, read the class ID from the structure.
            return MemoryManager.Global.Read<ClassID>(clientClass + 0x14);
        }
    }
}
