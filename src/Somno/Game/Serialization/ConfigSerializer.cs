using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Somno.LanguageExtensions;
using System.Threading.Tasks;

namespace Somno.Game.Serialization
{
    /// <summary>
    /// Manages the serialization of configuration values to persistent data storage.
    /// </summary>
    internal class ConfigSerializer
    {
        readonly BinaryWriter bw;
        readonly FileStream fs;

        ConfigSerializer(string name)
        {
            fs = new FileStream($"./config/{name}.cfg", FileMode.Create);
            bw = new(fs);
        }

        /// <summary>
        /// Begins serializing a configuration file under the given name.
        /// The configuration file will be saved in the local config folder.
        /// </summary>
        /// <param name="name">The name of the configuration set.</param>
        public static ConfigSerializer Serialize(string name)
        {
            return new ConfigSerializer(name);
        }

        public ConfigSerializer Write(int v)
        {
            bw.Write(v);
            return this;
        }

        public ConfigSerializer Write(Vector3 v)
        {
            bw.Write(v);
            return this;
        }

        public ConfigSerializer Write(Vector4 v)
        {
            bw.Write(v);
            return this;
        }

        public ConfigSerializer Write(float v)
        {
            bw.Write(v);
            return this;
        }

        public ConfigSerializer Write(bool v)
        {
            bw.Write(v);
            return this;
        }

        /// <summary>
        /// Finishes writing all of the values to the configuration file,
        /// and safely closes and disposes all native resources.
        /// </summary>
        public void Finish()
        {
            bw.Dispose();
            fs.Dispose();
        }
    }
}
