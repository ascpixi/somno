using Somno.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Somno.Game
{
    internal static class ModEngine
    {
        readonly static Dictionary<Type, GameModification> modTypeMap = new();

        public readonly static List<GameModification> Modifications = new();

        public static void AddModification<T>(T modification) where T : GameModification
        {
            if(modification is IOverlayRenderable overlayModule) {
                SomnoOverlay.Instance!.AddModule(overlayModule);
            }

            if(modification is IConfigRenderable configModule) {
                ConfigurationGUI.Instance!.AddConfigurable(configModule);
            }

            Modifications.Add(modification);
            modTypeMap.Add(typeof(T), modification);
        }

        public static T GetModification<T>() where T : GameModification
            => (T)modTypeMap[typeof(T)];

    }
}
