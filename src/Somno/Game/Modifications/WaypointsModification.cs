using ImGuiNET;
using Somno.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Somno.LanguageExtensions;

namespace Somno.Game.Modifications
{
    public class Waypoint
    {
        public Vector3 Position;
        public bool VisibleThroughWalls;
        public int ItemID;
        public Vector3 Color;

        public float X => Position.X;
        public float Y => Position.Y;
        public float Z => Position.Z;

        public float R => Color.X;
        public float G => Color.Y;
        public float B => Color.Z;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Position);
            writer.Write(VisibleThroughWalls);
            writer.Write(ItemID);
            writer.Write(Color);
        }

        public static Waypoint Deserialize(BinaryReader reader)
        {
            return new Waypoint() {
                Position = reader.ReadVector3(),
                VisibleThroughWalls = reader.ReadBoolean(),
                ItemID = reader.ReadInt32(),
                Color = reader.ReadVector3()
            };
        }
    }

    internal class WaypointsModification : IOverlayRenderable, IConfigRenderable
    {
        public bool Enabled;
        public bool ShowWaypointManager;
        public Dictionary<string, List<Waypoint>> MapWaypoints = new() {
            { "de_dust2", new() {
                new Waypoint() { Position = new(4, 7, 3), ItemID = 5, VisibleThroughWalls = true },
                new Waypoint() { Position = new(4, 7, 56), ItemID = 5, VisibleThroughWalls = true },
                new Waypoint() { Position = new(15, 7, 3), ItemID = 4, VisibleThroughWalls = false },
                new Waypoint() { Position = new(4, 67, 3), ItemID = 5, VisibleThroughWalls = true },
            } },

            { "de_nuke", new() {
                new Waypoint() { Position = new(4, 7, 3), ItemID = 5, VisibleThroughWalls = true },
                new Waypoint() { Position = new(4, 7, 56), ItemID = 5, VisibleThroughWalls = true },
                new Waypoint() { Position = new(15, 7, 3), ItemID = 4, VisibleThroughWalls = false },
                new Waypoint() { Position = new(4, 67, 3), ItemID = 5, VisibleThroughWalls = true },
            } }
        };

        bool createOnlyWithCurrentItem;
        bool createVisibleThroughWalls;
        bool createOnlyOnCurrentTeam;
        Vector3 createColor;
        Waypoint? waypointManagerSelected;

        public void RenderConfiguration(SomnoOverlay overlay)
        {
            if (ImGui.CollapsingHeader("Waypoints")) {
                ImGui.Text("Renders various waypoints in the game world.");
                ImGui.Indent(12);

                ImGui.Checkbox("Enabled", ref Enabled);

                if(ImGui.Button("Waypoint Manager"))
                    ShowWaypointManager = true;

                ImGui.SeparatorText("Create");
                ImGui.Checkbox("Only with current item", ref createOnlyWithCurrentItem);
                ImGui.Checkbox("Only on current team", ref createOnlyOnCurrentTeam);
                ImGui.Checkbox("Visible through walls", ref createVisibleThroughWalls);
                ImGui.ColorEdit3("Color", ref createColor);

                if(ImGui.Button("Create waypoint here"))
                    CreateAtPlayerPos();

                ImGui.Unindent(12);
            }

            if (ShowWaypointManager) {
                ImGui.Begin("Waypoint Manager");

                foreach (var item in MapWaypoints) {
                    if(ImGui.CollapsingHeader(item.Key)) {
                        ImGui.Indent(12);

                        if (ImGui.BeginListBox(item.Key)) {
                            foreach (var waypoint in item.Value) {
                                bool changed = ImGui.Selectable(
                                    $"({waypoint.X}, {waypoint.Y}, {waypoint.Z}), item {waypoint.ItemID}",
                                    waypointManagerSelected == waypoint
                                );

                                if (changed)
                                    waypointManagerSelected = waypoint;
                            }
                        }

                        ImGui.EndListBox();

                        if (waypointManagerSelected != null) {
                            if (ImGui.Button("Remove")) {
                                MapWaypoints[item.Key].Remove(waypointManagerSelected);
                                waypointManagerSelected = null;
                            }
                        }

                        ImGui.Unindent(12);
                    }
                }

                ImGui.Separator();

                if(ImGui.Button("Close"))
                    ShowWaypointManager = false;

                ImGui.End();
            }
        }

        public void CreateAtPlayerPos()
        {

        }

        public void RenderOnOverlay(SomnoOverlay overlay)
        {
            // ...
        }
    }
}
