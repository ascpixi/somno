using ImGuiNET;
using Somno.Game.Entities;
using Somno.Game.Rendering;
using Somno.Game.Serialization;
using Somno.Game.SourceEngine;
using Somno.LanguageExtensions;
using Somno.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Somno.Game.Modifications
{
    internal class ESPModification : GameModification, IOverlayRenderable, IConfigRenderable
    {
        public bool Enabled;

        public bool RenderTeam;
        public Vector3 TeamColor = new(0, 1, 0);

        public bool RenderEnemy = true;
        public Vector3 EnemyColor = new(1, 0, 0);

        public bool RenderBomb = true;
        public Vector3 BombColor = new(0, 1, 1);

        public bool RenderHitBoxes = true;
        public float HitBoxOpacity = 0.25f;
        public float HitBoxHeadOpacity = 0.75f;

        public int CornerSize = 12;
        public int HealthMeterX = 10;
        public int HealthMeterWidth = 2;
        public float BoxOpacity = 0.1f;

        float cutoff;

        public bool OverlayRenderDependsOnGame => true;

        public ESPModification()
        {
            if(ConfigDeserializer.Exists("esp")) {
                ConfigDeserializer.Deserialize("esp")
                    .ReadBool(x => Enabled = x)
                    .ReadBool(x => RenderTeam = x).ReadVector3(x => TeamColor = x)
                    .ReadBool(x => RenderEnemy = x).ReadVector3(x => EnemyColor = x)
                    .ReadBool(x => RenderBomb = x).ReadVector3(x => BombColor = x)
                    .ReadBool(x => RenderHitBoxes = x)
                        .ReadFloat(x => HitBoxOpacity = x)
                        .ReadFloat(x => HitBoxHeadOpacity = x)
                    .ReadInt32(x => CornerSize = x)
                    .ReadInt32(x => HealthMeterX = x).ReadInt32(x => HealthMeterWidth = x)
                    .ReadFloat(x => BoxOpacity = x)
                    .Finish();
            }
        }

        public void RenderConfiguration(SomnoOverlay overlay)
        {
            if (ImGui.CollapsingHeader("ESP")) {
                ImGui.Text("Renders the positions of players through walls.");
                ImGui.Indent(12);

                ImGui.Checkbox("Enabled", ref Enabled);

                ImGui.Checkbox("Render Enemies", ref RenderEnemy);
                if (RenderEnemy) {
                    ImGui.Indent();
                    ImGui.ColorEdit3("Enemy Color", ref EnemyColor);
                    ImGui.Unindent();
                }

                ImGui.Checkbox("Render Teammates", ref RenderTeam);
                if (RenderTeam) {
                    ImGui.Indent();
                    ImGui.ColorEdit3("Team Color", ref TeamColor);
                    ImGui.Unindent();
                }

                ImGui.Checkbox("Render Bomb", ref RenderBomb);
                if (RenderBomb) {
                    ImGui.Indent();
                    ImGui.ColorEdit3("Bomb Color", ref BombColor);
                    ImGui.Unindent();
                }

                ImGui.Checkbox("Render Hitboxes", ref RenderHitBoxes);
                if (RenderHitBoxes) {
                    ImGui.Indent();
                    ImGui.SliderFloat("Opacity", ref HitBoxOpacity, 0f, 1f);
                    ImGui.SliderFloat("Head Opacity", ref HitBoxHeadOpacity, 0f, 1f);
                    ImGui.Unindent();
                }

                ImGui.SliderInt("Corner Size", ref CornerSize, 0, 30);
                ImGui.SliderInt("Health Meter Width", ref HealthMeterWidth, 0, 10);
                ImGui.SliderInt("Health Meter X", ref HealthMeterX, -20, 20);
                ImGui.SliderFloat("Box Opacity", ref BoxOpacity, 0f, 1f);

                if(ImGui.Button("Save configuration")) {
                    ConfigSerializer.Serialize("esp")
                        .Write(Enabled)
                        .Write(RenderTeam).Write(TeamColor)
                        .Write(RenderEnemy).Write(EnemyColor)
                        .Write(RenderBomb).Write(BombColor)
                        .Write(RenderHitBoxes)
                            .Write(HitBoxOpacity)
                            .Write(HitBoxHeadOpacity)
                        .Write(CornerSize)
                        .Write(HealthMeterX).Write(HealthMeterWidth)
                        .Write(BoxOpacity)
                        .Finish();
                }

                ImGui.Unindent(12);
            }
        }

        public void RenderOnOverlay(SomnoOverlay overlay)
        {
            if(!Enabled) {
                return;
            }

            var overlaySize = overlay.HostWindow.Dimensions;
            
            ImGui.SetNextWindowContentSize(new(overlaySize.Width, overlaySize.Height));
            ImGui.SetNextWindowPos(new(0, 0));
            ImGui.Begin("(ESP Overlay)", ImGuiHelpers.RenderSurface);

            var render = ImGui.GetWindowDrawList();

            foreach (var player in GameManager.Players) {
                if (!player.Dormant || player.Health <= 0)
                    continue;

                Vector3 color;
                if (GameManager.CurrentPlayer.Team == player.Team) {
                    if (!RenderTeam)
                        continue;

                    color = TeamColor;
                } else {
                    if (!RenderEnemy)
                        continue;

                    color = EnemyColor;
                }

                float pheight = player.Crouching ? PlayerEntity.HeightCrouching : PlayerEntity.Height;

                var pposTopW = player.Position.AddZ(pheight);

                if (!Camera.WorldToScreen(pposTopW, out var pposTopS))
                    continue;

                var pposBottomW = player.Position.AddZ(-2f);

                if (!Camera.WorldToScreen(pposBottomW, out var pposBottomS))
                    continue;

                //render.AddCircleFilled(pposTopS, 4f, 0xFFFFFFFF);
                //render.AddCircleFilled(pposBottomS, 4f, 0xFFFFFFFF);

                var delta = pposTopS.Y - pposBottomS.Y;
                var p1 = pposTopS + new Vector2(delta * 0.2f, 0);
                var p2 = pposBottomS + new Vector2(-delta * 0.2f, 0);

                //render.AddCircleFilled(espp1, 4f, 0xFF0000FF);
                //render.AddCircleFilled(espp2, 4f, 0xFF0000FF);

                //render.AddRect(p1, p2, ImGui.ColorConvertFloat4ToU32(new Vector4(color, 1)));

                uint color32 = ImGui.ColorConvertFloat4ToU32(new(color, 1));

                float scaling = Math.Clamp(-delta / (768 / 2), 0.15f, 1f);
                // cubic
                scaling -= 1;
                scaling = scaling * scaling * scaling;
                scaling += 1;

                float sign = p1.X < p2.X ? -1 : 1;
                float cornerSize = CornerSize * scaling;
                float cornerSizeP = cornerSize * sign;
                float cornerSizeN = -cornerSizeP;

                render.AddLine(new(p1.X, p1.Y), new(p1.X + cornerSizeN, p1.Y), color32);
                render.AddLine(new(p1.X, p1.Y), new(p1.X, p1.Y + cornerSize), color32);

                render.AddLine(new(p2.X, p2.Y), new(p2.X + cornerSizeP, p2.Y), color32);
                render.AddLine(new(p2.X, p2.Y), new(p2.X, p2.Y - cornerSize), color32);

                uint bgColor32 = ImGui.ColorConvertFloat4ToU32(new(color, BoxOpacity));
                render.AddRect(p1, p2, bgColor32);

                // Render health
                var hp = Math.Clamp(player.Health / 100f, 0, 1);
                var hpColor = new Vector4(
                    Math.Clamp(2.0f * (1 - hp), 0, 1),
                    Math.Clamp(2.0f * hp, 0, 1),
                    0f, 1f
                );
                var hpColor32 = ImGui.ColorConvertFloat4ToU32(hpColor);

                float healthX = p1.X + (HealthMeterX * sign);
                float meterHeight = p2.Y - p1.Y;
                float yOffset = (1.0f - hp) * meterHeight;
                render.AddRectFilled(new(healthX, p2.Y), new(healthX + HealthMeterWidth, p1.Y + yOffset), hpColor32);

                if (RenderHitBoxes) {
                    var hbColor32 = ImGui.ColorConvertFloat4ToU32(new(color, HitBoxOpacity));
                    var hbhColor32 = ImGui.ColorConvertFloat4ToU32(new(color, HitBoxHeadOpacity));
                    DrawHitboxes(player, hbColor32, hbhColor32);
                }
            }

            if (RenderBomb && GameManager.BombPlanted) {
                if(Camera.WorldToScreen(GameManager.Bomb.Position, out var wPos)) {
                    var col32 = ImGui.ColorConvertFloat4ToU32(new(BombColor, 1));
                    render.AddCircleFilled(wPos, 3f, col32);
                }
            }

            ImGui.End();
        }

        static void DrawHitboxes(PlayerEntity player, uint color, uint headColor)
        {
            float dist = Vector3.DistanceSquared(player.Position, GameManager.CurrentPlayer.Position);
            if (dist < 300 * 300) {
                return;
            }

            for (var i = 0; i < player.Hitboxes.Length; i++) {
                var hitbox = player.Hitboxes[i];
                if (hitbox.Bone is < 0 or > MStudioBBox.MaxStudioBones) {
                    return;
                }

                uint thisColor = color;
                if (hitbox.BBMin.Y > 1.25f) {
                    thisColor = headColor;
                }

                if (hitbox.Radius > 0) {
                    DrawHitBoxCapsule(player, thisColor, i);
                }
            }
        }

        static void DrawHitBoxCapsule(PlayerEntity player, uint color, int hitBoxId)
        {
            var hitbox = player.Hitboxes[hitBoxId];
            var mBoneModelToWorld = player.BoneMatrices[hitbox.Bone].To4x4();

            var bonePos0World = Vector3.Transform(hitbox.BBMin, mBoneModelToWorld);
            var bonePos1World = Vector3.Transform(hitbox.BBMax, mBoneModelToWorld);

            var render = ImGui.GetBackgroundDrawList();
            render.AddCapsuleWorld(color, bonePos0World, bonePos1World, hitbox.Radius, 6, 3);
        }
    }
}
