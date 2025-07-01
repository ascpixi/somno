using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game
{
    /// <summary>
    /// Source Engine-related mathematical functions.
    /// </summary>
    internal static class MathS
    {
        public static float Deg2Rad(float x) => ((float)(x) * (float)(MathF.PI / 180f));

        public static Vector3 GetForward(Vector3 angles)
        {
            float sp, sy, cp, cy;

            (sy, cy) = MathF.SinCos(Deg2Rad(angles.Y));
            (sp, cp) = MathF.SinCos(Deg2Rad(angles.X));

            return new(cp * cy, cp * sy, -sp);
        }
    }
}
