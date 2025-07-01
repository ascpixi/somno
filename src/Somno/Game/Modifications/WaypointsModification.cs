using ImGuiNET;
using Somno.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Somno.LanguageExtensions;
using Somno.Game.Entities;

namespace Somno.Game.Modifications;

internal class Waypoint
{
    public Vector3 Position;
    public WeaponType ItemID;
    public uint Color;
    public int Size;
    public TeamType Team;
    public float MaximumDistance;

    public float X => Position.X;
    public float Y => Position.Y;
    public float Z => Position.Z;

    public bool Itembound => ItemID != WeaponType.Undefined;
    public bool Teambound => ((byte)Team & (1 << 7)) == 0;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Position);
        writer.Write((ushort)ItemID);
        writer.Write(Color);
        writer.Write(Size);
        writer.Write((byte)Team);
        writer.Write(MaximumDistance);
    }

    public static Waypoint Deserialize(BinaryReader reader)
    {
        return new Waypoint() {
            Position = reader.ReadVector3(),
            ItemID = (WeaponType)reader.ReadUInt16(),
            Color = reader.ReadUInt32(),
            Size = reader.ReadInt32(),
            Team = (TeamType)reader.ReadByte(),
            MaximumDistance = reader.ReadSingle()
        };
    }
}

internal class WaypointsModification : GameModification, IOverlayRenderable, IConfigRenderable
{
    public bool Enabled;
    public bool ShowWaypointManager;
    public Dictionary<string, List<Waypoint>> MapWaypoints = new();
    
    public bool OverlayRenderDependsOnGame => true;

    bool createOnlyWithCurrentItem;
    bool createOnlyOnCurrentTeam;
    Vector4 createColor = new(1, 0, 0, 1);
    int createSize = 6;
    float createMaxDistance = 2000;
    Vector3 createPos = new(0, 0, 0);
    Vector3 createOffset = new(0, 0, 0);
    Waypoint? waypointManagerSelected;
    bool visualizeCreate;

    string? mapName;
    List<Waypoint>? activeWaypointSet;

    public WaypointsModification()
    {
        if(File.Exists("./config/waypoints.cfg")) {
            DeserializeWaypoints();
        }
    }

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
            ImGui.ColorEdit4("Color", ref createColor);
            ImGui.SliderInt("Size", ref createSize, 0, 32);
            ImGui.SliderFloat("Maximum Distance", ref createMaxDistance, 100, 10000);
            ImGui.InputFloat3("Position", ref createPos);

            ImGui.SameLine();
            if (ImGui.Button("Here")) {
                createPos = GameManager.CurrentPlayer.Position;
            }

            ImGui.SliderFloat3("Offset", ref createOffset, -100, 100);
            ImGui.Checkbox("Visualize", ref visualizeCreate);

            if (mapName == null || GameManager.Map == null) ImGui.BeginDisabled();

            if(ImGui.Button("Create waypoint here"))
                CreateWaypoint();

            if (mapName == null || GameManager.Map == null) ImGui.EndDisabled();

            if (!GameManager.Playing || GameManager.Map == null) ImGui.BeginDisabled();
            ImGui.SameLine();
            if(ImGui.Button("Load waypoints for map")) {
                mapName = GameManager.Map!.Name;
                activeWaypointSet = MapWaypoints.GetValueOrDefault(mapName);
            }
            if (!GameManager.Playing || GameManager.Map == null) ImGui.EndDisabled();

            ImGui.SameLine();
            if(ImGui.Button("Save")) {
                SerializeWaypoints();
            }

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

    void CreateWaypoint()
    {
        if(mapName == null)
            throw new InvalidOperationException("Cannot create a waypoint, while a waypoint set isn't loaded.");

        if (!MapWaypoints.TryGetValue(mapName, out List<Waypoint>? waypoints)) {
            waypoints = new();
            activeWaypointSet = waypoints;
            MapWaypoints.Add(mapName, waypoints);
        }

        var p = GameManager.CurrentPlayer;
        waypoints.Add(new Waypoint() {
            Color = ImGui.ColorConvertFloat4ToU32(createColor),
            ItemID = createOnlyWithCurrentItem ? p.Weapon : WeaponType.Undefined,
            Position = createPos + createOffset,
            Size = createSize,
            Team = createOnlyOnCurrentTeam ? p.Team : TeamType.Undefined,
            MaximumDistance = createMaxDistance
        });
    }

    public void SerializeWaypoints()
    {
        using var fs = new FileStream("./config/waypoints.cfg", FileMode.Create);
        using var bw = new BinaryWriter(fs);
        bw.Write(Enabled);

        bw.Write(MapWaypoints.Count);
        foreach (var kv in MapWaypoints) {
            bw.Write(kv.Key);
            bw.Write(kv.Value.Count);

            foreach (var waypoint in kv.Value) {
                waypoint.Serialize(bw);
            }
        }
    }

    public void DeserializeWaypoints()
    {
        using var fs = new FileStream("./config/waypoints.cfg", FileMode.Open);
        using var br = new BinaryReader(fs);
        Enabled = br.ReadBoolean();

        int mapCount = br.ReadInt32();
        MapWaypoints = new(mapCount);

        for (int i = 0; i < mapCount; i++) {
            var map = br.ReadString();
            int waypointCount = br.ReadInt32();

            var waypoints = new List<Waypoint>(waypointCount);

            for (int j = 0; j < waypointCount; j++) {
                waypoints.Add(Waypoint.Deserialize(br));
            }

            MapWaypoints[map] = waypoints;
        }
    }

    public void RenderOnOverlay(SomnoOverlay overlay)
    {
        if (!Enabled)
            return;

        var overlaySize = SomnoOverlay.Instance!.HostWindow.Dimensions;

        ImGui.SetNextWindowContentSize(new(overlaySize.Width, overlaySize.Height));
        ImGui.SetNextWindowPos(new(0, 0));
        ImGui.Begin("(Waypoints Overlay)", ImGuiHelpers.RenderSurface);

        var render = ImGui.GetWindowDrawList();

        if (activeWaypointSet == null)
            return;

        if (visualizeCreate) {
            var pos = createPos + createOffset;
            if (Camera.WorldToScreen(pos, out var screenPos)) {
                var c32 = ImGui.ColorConvertFloat4ToU32(createColor * new Vector4(1, 1, 1, 0.5f));
                render.AddCircleFilled(screenPos, createSize, c32);
            }
        }

        var player = GameManager.CurrentPlayer;
        foreach (var waypoint in activeWaypointSet) {
            if (waypoint.Itembound && player.Weapon != waypoint.ItemID)
                continue;

            if (waypoint.Teambound && player.Team != waypoint.Team)
                continue;

            if (!Camera.WorldToScreen(waypoint.Position, out var screenPos))
                continue;

            float maxDistSqr = waypoint.MaximumDistance * waypoint.MaximumDistance;
            float distSqr = Vector3.DistanceSquared(waypoint.Position, player.Position);
            if (distSqr > maxDistSqr)
                continue;

            render.AddCircleFilled(screenPos, waypoint.Size, waypoint.Color);
        }

        ImGui.End();
    }
}
