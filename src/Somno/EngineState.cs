using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno
{
    /// <summary>
    /// Represents the state of the overall Somno game modification engine.
    /// </summary>
    internal enum EngineState
    {
        /// <summary>
        /// The engine is not yet initialized.
        /// </summary>
        NotInitialized = 0,

        /// <summary>
        /// The engine is waiting for the target process to start.
        /// </summary>
        WaitingForProcess = 1,

        /// <summary>
        /// The engine is currently running.
        /// </summary>
        Running = 2,

        /// <summary>
        /// The engine is terminating, and is currently performing
        /// clean-up tasks. All threads that observe the engine state
        /// should terminate after encountering this value.
        /// </summary>
        Terminating = 3,

        /// <summary>
        /// The engine has been terminated.
        /// </summary>
        Terminated = 4,
    }
}
