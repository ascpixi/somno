namespace Somno.Native.NT;

internal enum ObjectInformationClass : int
{
    ObjectBasicInformation = 0,
    ObjectNameInformation = 1,
    ObjectTypeInformation = 2,
    ObjectAllTypesInformation = 3,
    ObjectHandleInformation = 4
}
