using Somno.Native.WinUSER;
using System.Drawing;

namespace Somno.UI.Engine.GDI;

/// <summary>
/// Represents a Windows window.
/// </summary>
internal sealed class Win32Window
{
    public nint Handle;
    public Rectangle Dimensions;

    /// <summary>
    /// Creates a wrapper over the given window.
    /// </summary>
    /// <param name="hwnd">The handle of the target window.</param>
    public Win32Window(nint hwnd)
    {
        Handle = hwnd;
        
        User32.GetWindowRect(hwnd, out RECT rect);
        Dimensions = new Rectangle(rect.Location, rect.Size);
    }
}
