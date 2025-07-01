using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game
{
    /// <summary>
    /// Thrown when the game process has been closed, and a method operating
    /// on said process is called.
    /// </summary>
    internal class GameClosedException : Exception
    {
        public GameClosedException() : base("The game process has been closed.") { }
    }
}
