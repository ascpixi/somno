using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.IPC
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct PortalIPCRegion
    {
        /// <summary>
        /// Set to <see langword="true"/> if the controller has created a request and is waiting for
        /// a response. The portal needs to set this value to <see langword="false"/> in order to notify
        /// the controller that the request has been fulfilled.
        /// </summary>
        public bool PendingRequest;

        /// <summary>
        /// The type of action to perform.
        /// </summary>
        public PortalIPCRequest RequestID;

        public fixed byte Payload[32]; // this would usually be a fixed struct but NAOT generates invalid IL for some reason

        public ulong ReadPayload64()
        {
            fixed (byte* p = Payload)
                return *(ulong*)p;
        }

        public ulong ReadPayload64(ref int counter)
        {
            fixed (byte* p = Payload) {
                var start = p + counter;
                counter += sizeof(ulong);
                return *(ulong*)start;
            }
        }

        public void WritePayload(ulong value, ref int counter)
        {
            fixed(byte* p = Payload) {
                var start = p + counter;
                *(ulong*)start = value;
                counter += sizeof(ulong);
            }
        }

        public void WritePayload(ulong value)
        {
            fixed (byte* p = Payload)
                *(ulong*)p = value;
        }

        public void WritePayload(uint value, ref int counter)
        {
            fixed (byte* p = Payload) {
                var start = p + counter;
                *(uint*)start = value;
                counter += sizeof(uint);
            }
        }

        public void WritePayload(uint value)
        {
            fixed (byte* p = Payload)
                *(uint*)p = value;
        }
    }
}
