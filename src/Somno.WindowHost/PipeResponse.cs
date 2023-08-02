using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.WindowHost
{
    [Flags]
    public enum PipeResponse : byte
    {
        /// <summary>
        /// The message was handled. The HResult will follow the response.
        /// </summary>
        Handled = (1 << 0),

        /// <summary>
        /// The cursor should be changed to the given system resource.
        /// The 16-bit identifier will follow the response, after the HResult.
        /// </summary>
        ChangeCursor = (1 << 1),

        /// <summary>
        /// The display affinity of the window should be changed, so that
        /// screen capture does not include the window.
        /// </summary>
        HideFromCapture = (1 << 2),

        /// <summary>
        /// The display affinity of the window should be changed, so that
        /// screen capture does include the window.
        /// </summary>
        ShowOnCapture = (1 << 3),
    }
}
