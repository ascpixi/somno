namespace Somno.Native;

internal static partial class Kernel32
{
    public const nint InvalidHandleValue = -1; 

    public const uint PageReadWrite = 0x04;
    public const uint SecCommit = 0x08000000;
    public const uint SecNoCache = 0x10000000;

    public const uint StandardRightsRequired = 0x000F0000;
    public const uint SectionQuery = 0x1;
    public const uint SectionMapWrite = 0x2;
    public const uint SectionMapRead = 0x4;
    public const uint SectionMapExecute = 0x8;
    public const uint SectionExtendSize = 0x10;

    public const uint GenericRead = 0x80000000;
    public const uint GenericWrite = 0x40000000;

    public const uint FileShareRead = 1;
    public const uint FileShareWrite = 2;

    public const uint OpenExisting = 3;

    public const uint FileMapAllAccess =
        StandardRightsRequired |
        SectionQuery |
        SectionMapWrite |
        SectionMapRead |
        SectionMapExecute |
        SectionExtendSize;                  
}
