using Somno.Portal.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Portal.Windows
{
    /// <summary>
    /// Represents an open handle.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal readonly struct Handle
    {
        [FieldOffset(0)] public readonly IntPtr Value;

        /// <summary>
        /// Represents the value of a handle that is invalid. (INVALID_HANDLE_VALUE)
        /// </summary>
        public const IntPtr InvalidValue = -1;

        public Handle(IntPtr value)
        {
            Value = value;
        }

        /// <summary>
        /// Closes this handle if it is valid.
        /// </summary>
        public void TryClose()
        {
            if(!IsInvalidOrNull) {
                Kernel32.CloseHandle(Value);
            }
        }

        public bool IsNull => Value == 0;
        public bool IsInvalid => Value == InvalidValue;
        public bool IsInvalidOrNull => Value is 0 or InvalidValue;

        public static implicit operator IntPtr(Handle h) => h.Value;
        public static implicit operator Handle(IntPtr v) => new(v);

        public static bool operator ==(Handle lhs, Handle rhs) => lhs.Value == rhs.Value;
        public static bool operator !=(Handle lhs, Handle rhs) => lhs.Value != rhs.Value;

        public static bool operator ==(Handle lhs, IntPtr rhs) => lhs.Value == rhs;
        public static bool operator !=(Handle lhs, IntPtr rhs) => lhs.Value != rhs;

        public static bool operator ==(Handle lhs, ulong rhs) => lhs.Value == (nint)rhs;
        public static bool operator !=(Handle lhs, ulong rhs) => lhs.Value != (nint)rhs;

        public static bool operator ==(Handle lhs, long rhs) => lhs.Value == (nint)rhs;
        public static bool operator !=(Handle lhs, long rhs) => lhs.Value != (nint)rhs;

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;

            if (obj is Handle h1) return this == h1;
            if (obj is IntPtr h2) return this == h2;
            if (obj is ulong h3) return this == h3;
            if (obj is long h4) return this == h4;

            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => $"(handle 0x{Value:X2})";
    }
}
