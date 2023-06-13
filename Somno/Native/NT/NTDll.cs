using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Somno.Native.NT
{
    internal class NTDll
    {
        /// <summary>
        /// Retrieves the specified system information.
        /// </summary>
        /// <param name="systemInformationClass">One of the values enumerated in SYSTEM_INFORMATION_CLASS, which indicate the kind of system information to be retrieved</param>
        /// <param name="systemInformation">A pointer to a buffer that receives the requested information. The size and structure of this information varies depending on the value of the SystemInformationClass parameter</param>
        /// <param name="systemInformationLength">The size of the buffer pointed to by the SystemInformation parameter, in bytes</param>
        /// <param name="returnLength">An optional pointer to a location where the function writes the actual size of the information requested. If that size is less than or equal to the SystemInformationLength parameter, the function copies the information into the SystemInformation buffer; otherwise, it returns an NTSTATUS error code and returns in ReturnLength the size of buffer required to receive the requested information.</param>
        /// <returns>Returns an NTSTATUS success or error code.</returns>
        [DllImport("ntdll.dll")]
        public static unsafe extern NTStatus NtQuerySystemInformation(
            SystemInformationClass systemInformationClass,
            void* systemInformation,
            uint systemInformationLength,
            out uint returnLength
        );

        /// <summary>
        /// Retrieves various kinds of object information.
        /// </summary>
        /// <param name="handle">The handle of the object for which information is being queried.</param>
        /// <param name="objectInformationClass">One of the following values, as enumerated in OBJECT_INFORMATION_CLASS, indicating the kind of object information to be retrieved.</param>
        /// <param name="objectInformation">An optional pointer to a buffer where the requested information is to be returned. The size and structure of this information varies depending on the value of the ObjectInformationClass parameter.</param>
        /// <param name="objectInformationLength">The size of the buffer pointed to by the ObjectInformation parameter, in bytes.</param>
        /// <param name="returnLength">An optional pointer to a location where the function writes the actual size of the information requested. If that size is less than or equal to the ObjectInformationLength parameter, the function copies the information into the ObjectInformation buffer; otherwise, it returns an NTSTATUS error code and returns in ReturnLength the size of the buffer required to receive the requested information.</param>
        /// <returns>Returns an NTSTATUS or error code.</returns>
        [DllImport("ntdll.dll")]
        public static unsafe extern NTStatus NtQueryObject(
            IntPtr handle,
            ObjectInformationClass objectInformationClass,
            void* objectInformation,
            uint objectInformationLength,
            out uint returnLength
        );
    }
}
