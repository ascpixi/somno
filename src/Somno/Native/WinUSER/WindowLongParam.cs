namespace Somno.Native.WinUSER;

internal enum WindowLongParam
{
    /// <summary>Sets a new address for the window procedure.</summary>
    /// <remarks>You cannot change this attribute if the window does not belong to the same process as the calling thread.</remarks>
    GWL_WNDPROC = -4,

    /// <summary>Sets a new application instance handle.</summary>
    GWLP_HINSTANCE = -6,

    GWLP_HWNDPARENT = -8,

    /// <summary>Sets a new identifier of the child window.</summary>
    /// <remarks>The window cannot be a top-level window.</remarks>
    GWL_ID = -12,

    /// <summary>Sets a new window style.</summary>
    GWL_STYLE = -16,

    /// <summary>Sets a new extended window style.</summary>
    /// <remarks>See <see cref="ExWindowStyles"/>.</remarks>
    GWL_EXSTYLE = -20,

    /// <summary>Sets the user data associated with the window.</summary>
    /// <remarks>This data is intended for use by the application that created the window. Its value is initially zero.</remarks>
    GWL_USERDATA = -21,

    /// <summary>Sets the return value of a message processed in the dialog box procedure.</summary>
    /// <remarks>Only applies to dialog boxes.</remarks>
    DWLP_MSGRESULT = 0,

    /// <summary>Sets new extra information that is private to the application, such as handles or pointers.</summary>
    /// <remarks>Only applies to dialog boxes.</remarks>
    DWLP_USER = 8,

    /// <summary>Sets the new address of the dialog box procedure.</summary>
    /// <remarks>Only applies to dialog boxes.</remarks>
    DWLP_DLGPROC = 4
}
