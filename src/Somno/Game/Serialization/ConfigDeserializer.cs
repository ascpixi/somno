using Somno.LanguageExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game.Serialization
{
    /// <summary>
    /// Deserializes previously serialized configuration files.
    /// </summary>
    internal class ConfigDeserializer
    {
        readonly BinaryReader br;
        readonly FileStream fs;

        ConfigDeserializer(string name)
        {
            fs = new FileStream($"./config/{name}.cfg", FileMode.Open);
            br = new(fs);
        }

        public static bool Exists(string name) => File.Exists($"./config/{name}.cfg");

        /// <summary>
        /// Begins deserializing a configuration file under the given name.
        /// The configuration file will be loaded from the local config folder.
        /// </summary>
        /// <param name="name">The name of the configuration set.</param>
        public static ConfigDeserializer Deserialize(string name)
        {
            return new ConfigDeserializer(name);
        }

        public ConfigDeserializer ReadInt32(Action<int> f)
        {
            f(br.ReadInt32());
            return this;
        }

        public ConfigDeserializer ReadVector3(Action<Vector3> f)
        {
            f(br.ReadVector3());
            return this;
        }

        public ConfigDeserializer ReadVector4(Action<Vector4> f)
        {
            f(br.ReadVector4());
            return this;
        }

        public ConfigDeserializer ReadFloat(Action<float> f)
        {
            f(br.ReadSingle());
            return this;
        }

        public ConfigDeserializer ReadBool(Action<bool> f)
        {
            f(br.ReadBoolean());
            return this;
        }

        /// <summary>
        /// Finishes reading all of the values from the configuration file,
        /// and safely closes and disposes all native resources.
        /// </summary>
        public void Finish()
        {
            br.Dispose();
            fs.Dispose();
        }
    }
}
