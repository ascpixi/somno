using System.Runtime.InteropServices;

namespace Somno.WindowHost.Native.WinUSER;

/// <summary>
/// Contains window class information. It is used with the RegisterClassEx and GetClassInfoEx  functions.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WndClassExW
{
    /// <summary>
    /// The size, in bytes, of this structure. Set this member to sizeof(WNDCLASSEX). Be sure to set this member before calling the GetClassInfoEx function.
    /// </summary>
    public uint Size;

    /// <summary>
    /// The class style(s). This member can be any combination of the Class Styles.
    /// </summary>
    public WindowClassStyles Style;

    /// <summary>
    /// A pointer to the window procedure. You must use the CallWindowProc function to call the window procedure. For more information, see WindowProc.
    /// </summary>
    public delegate*<nint, uint, nuint, nint, nint> WndProc;

    /// <summary>
    /// The number of extra bytes to allocate following the window-class structure. The system initializes the bytes to zero.
    /// </summary>
    public int ClsExtra;

    /// <summary>
    /// The number of extra bytes to allocate following the window instance. The system initializes the bytes to zero. If an application uses WNDCLASSEX to register a dialog box created by using the CLASS directive in the resource file, it must set this member to DLGWINDOWEXTRA.
    /// </summary>
    public int WndExtra;

    /// <summary>
    /// A handle to the instance that contains the window procedure for the class.
    /// </summary>
    public nint Instance;

    /// <summary>
    /// A handle to the class icon. This member must be a handle to an icon resource. If this member is NULL, the system provides a default icon.
    /// </summary>
    public nint Icon;

    /// <summary>
    /// A handle to the class cursor. This member must be a handle to a cursor resource. If this member is NULL, an application must explicitly set the cursor shape whenever the mouse moves into the application's window.
    /// </summary>
    public nint Cursor;

    /// <summary>
    /// A handle to the class background brush. This member can be a handle to the brush to be used for painting the background, or it can be a color value. A color value must be one of the following standard system colors (the value 1 must be added to the chosen color).
    /// </summary>
    public nint Brush;

    /// <summary>
    /// Pointer to a null-terminated character string that specifies the resource name of the class menu, as the name appears in the resource file. If you use an integer to identify the menu, use the MAKEINTRESOURCE macro. If this member is NULL, windows belonging to this class have no default menu.
    /// </summary>
    public char* MenuName;

    /// <summary>
    /// A pointer to a null-terminated string or is an atom. If this parameter is an atom, it must be a class atom created by a previous call to the RegisterClass or RegisterClassEx function. The atom must be in the low-order word of lpszClassName; the high-order word must be zero.
    /// </summary>
    public char* ClassName;

    /// <summary>
    /// A handle to a small icon that is associated with the window class. If this member is NULL, the system searches the icon resource specified by the hIcon member for an icon of the appropriate size to use as the small icon.
    /// </summary>
    public nint IconSmall;
}
