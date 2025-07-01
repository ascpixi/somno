using ImGuiNET;
using Somno.LanguageExtensions;
using System.Numerics;

namespace Somno.Game.Rendering;

internal static class ImGui3DExtensions
{
    static void AddPolyline(this ImDrawListPtr render, uint color, Vector3[] positions)
    {
        var prev = positions[0].AsVec2().Add(-4, 0);
        for (int i = 1; i < positions.Length; i++) {
            var next = positions[i].AsVec2().Add(-4, 0);

            render.AddLine(prev, next, color);

            prev = next;
        }
    }

    public static void AddCapsuleWorld(this ImDrawListPtr render, uint color, Vector3 start, Vector3 end, float radius, int segments, int layers)
    {
        var normal = Vector3.Normalize(end - start);
        var halfSphere0 = Math3D.GetHalfSphere(start, -normal, radius, segments, layers);
        var halfSphere1 = Math3D.GetHalfSphere(end, normal, radius, segments, layers);

        // world to screen + draw layered circles
        for (var i = 0; i < layers; i++) {
            for (var j = 0; j < segments + 1; j++) {
                halfSphere0[i][j] = Camera.WorldToScreen(halfSphere0[i][j]).AsVec3();
                halfSphere1[i][j] = Camera.WorldToScreen(halfSphere1[i][j]).AsVec3();
            }

            render.AddPolyline(color, halfSphere0[i]);
            render.AddPolyline(color, halfSphere1[i]);
        }

        // draw verticals of half-spheres (connect layered circles)
        var halfSphereTopScreen0 = Camera.WorldToScreen(start - normal * radius).AsVec3();
        var halfSphereTopScreen1 = Camera.WorldToScreen(end + normal * radius).AsVec3();
        var verticals0 = new Vector3[layers + 1];
        var verticals1 = new Vector3[layers + 1];
        for (var vertexId = 0; vertexId < segments + 1; vertexId++) {
            for (var layerId = 0; layerId < layers; layerId++) {
                verticals0[layerId] = halfSphere0[layerId][vertexId];
                verticals1[layerId] = halfSphere1[layerId][vertexId];
            }
            verticals0[layers] = halfSphereTopScreen0;
            verticals1[layers] = halfSphereTopScreen1;

            render.AddPolyline(color, verticals0);
            render.AddPolyline(color, verticals1);
        }

        // draw vertical cylinder edges between half-spheres
        DrawCylinderSidesWorld(render, color, start, end, radius, segments);
    }

    private static void DrawCylinderSidesWorld(ImDrawListPtr render, uint color, Vector3 start, Vector3 end, float radius, int segments)
    {
        var normal = (end - start).Normalize();
        var vertices0 = Math3D.GetCircleVertices(start, normal, radius, segments);
        var vertices1 = Math3D.GetCircleVertices(end, normal, radius, segments);

        for (var i = 0; i < vertices0.Length; i++) {
            var p0 = Camera.WorldToScreen(vertices0[i]).Add(-4, 0);
            var p1 = Camera.WorldToScreen(vertices1[i]).Add(-4, 0);

            render.AddLine(p0, p1, color);
        }
    }
}
