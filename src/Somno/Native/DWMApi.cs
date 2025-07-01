using System;
using System.Runtime.InteropServices;

namespace Somno.Native;

/// <summary>
/// Exposes functions from the <c>DWMAPI</c> Dynamic-Link Library.
/// </summary>
internal static class DWMApi
{
    /// <summary>
    /// Extends the window frame into the client area.
    /// </summary>
    /// <param name="hWnd">The handle to the window in which the frame will be extended into the client area.</param>
    /// <param name="pMarInset">A pointer to a MARGINS structure that describes the margins to use when extending the frame into the client area.</param>
    /// <returns>If this function succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</returns>
    [DllImport("dwmapi.dll")]
    internal static extern int DwmExtendFrameIntoClientArea(
        IntPtr hWnd,
        ref DWMMargins pMarInset
    );
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct DWMMargins
{
    public readonly int Left;
    public readonly int Right;
    public readonly int Top;
    public readonly int Bottom;

    internal DWMMargins(int l)
    {
        this.Left = this.Right = this.Top = this.Bottom = l;
    }

    internal DWMMargins(int l, int r, int t, int b)
    {
        this.Left = l;
        this.Right = r;
        this.Top = t;
        this.Bottom = b;
    }
}
