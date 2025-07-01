using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace Somno.Game
{
    internal class Map
    {
        /// <summary>
        /// The name of the map, without the <c>.bsp</c> extension.
        /// </summary>
        public readonly string Name;

        public Map(string name)
        {
            Name = name;
        }
    }
}
