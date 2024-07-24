using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.STD;
using ImGuiNET;
using ImGuizmoNET;
using IVPlugin.ActorData;
using IVPlugin.Actors.SkeletonData;
using IVPlugin.Camera;
using IVPlugin.Core;
using IVPlugin.Core.Extentions;
using IVPlugin.Core.Files;
using IVPlugin.Json;
using IVPlugin.Log;
using IVPlugin.Posing;
using IVPlugin.Resources;
using IVPlugin.Services;
using IVPlugin.UI.Helpers;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static IVPlugin.Core.Files.PoseFile;

namespace IVPlugin.UI.Windows
{
    public static class SkeletonOverlay
    {
        public const string TransformName = "Model Transform";

        public static bool IsOpen = false;
        public static void Show(XIVActor actor)
        {
            mainActor = actor;
            availableBones.Clear();
            bones.Clear();
            clickedBones.Clear();
            selectableBones.Clear();
            SelectedBone = null!;
            IsOpen = true;
        }
        public static void Hide() => IsOpen = false;

        private static XIVActor mainActor;

        private static Dictionary<IllusioBone, ClickableBone> bones = new Dictionary<IllusioBone, ClickableBone>();

        private static List<Clickable> availableBones = new List<Clickable>();

        private static List<Clickable> clickedBones = new();
        private static List<Clickable> selectableBones = new();

        private static Clickable modelTransform = null;

        public static ClickableBone SelectedBone = null;

        private static bool disabled = false, forceDisabled = false, freezeAnim = false;

        private static Matrix4x4 projectionMatrix, viewMatrix, actorWorldViewMatrix, mainhandWVM, offhandWVM;

        private static OPERATION op = OPERATION.TRANSLATE;
        private static MODE mode = MODE.WORLD;
        private static MirrorModes mirror = MirrorModes.None;
        private static bool chain = true;

        private static bool showBase = true, showFace = true, showhair = true, showWeapon = true, showTertiary = true, showNSFW = false,
            showMouth = true, showEyes = true, showHead = true, showLegs = true, showTorso = true, showArms = true, showSkirt = true, showTail = true, showBuki = true, showIVCS = true,
            showIVCSPhys = false;

        private static float circleSize = 8, lineThickness = 2;
        

        public static void Draw()
        {
            if (!IsOpen) return;

            if (mainActor == null || !mainActor.IsLoaded())
            {
                Hide();
                return;
            }

            freezeAnim = PosingManager.Instance.frozen;

            unsafe
            {
                var cam = XIVCamera.instance.GetCurrentCamera();

                projectionMatrix = cam->GetProjectionMatrix();
                viewMatrix = cam->Camera.CameraBase.SceneCamera.ViewMatrix;
                viewMatrix.M44 = 1;

                var modelMatrix = new Transform()
                {
                    Position = (Vector3)mainActor.actorObject.Base()->DrawObject->Object.Position,
                    Rotation = (Quaternion)mainActor.actorObject.Base()->DrawObject->Object.Rotation,
                    Scale = mainActor.actorObject.Base()->DrawObject->Object.Scale * mainActor.GetActorScale()
                }.ToMatrix();

                if(mainActor.TryGetWeapon(DrawDataContainer.WeaponSlot.MainHand, out var mh))
                {
                    var weaponMatrix = new Transform()
                    {
                        Position = (Vector3)mh->drawData->CharacterBase.DrawObject.Object.Position,
                        Rotation = (Quaternion)mh->drawData->CharacterBase.DrawObject.Object.Rotation,
                        Scale = Vector3.Clamp((Vector3)mh->drawData->CharacterBase.DrawObject.Object.Scale * mh->drawData->ScaleFactor, new Vector3(.5f), new Vector3(1.5f))
                    }.ToMatrix();

                    mainhandWVM = weaponMatrix * viewMatrix;
                }

                if (mainActor.TryGetWeapon(DrawDataContainer.WeaponSlot.OffHand, out var oh))
                {
                    var weaponMatrix = new Transform()
                    {
                        Position = (Vector3)oh->drawData->CharacterBase.DrawObject.Object.Position,
                        Rotation = (Quaternion)oh->drawData->CharacterBase.DrawObject.Object.Rotation,
                        Scale = Vector3.Clamp((Vector3)oh->drawData->CharacterBase.DrawObject.Object.Scale * oh->drawData->ScaleFactor, new Vector3(.5f), new Vector3(1.5f))
                    }.ToMatrix();

                    offhandWVM = weaponMatrix * viewMatrix;
                }

                actorWorldViewMatrix = modelMatrix * viewMatrix;
            }

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));

            if (ImGui.Begin("IV Overlay", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs))
            {
                var io = ImGui.GetIO();
                ImGui.SetWindowSize(io.DisplaySize);


                var wp = ImGui.GetWindowPos();

                ImGuizmo.BeginFrame();

                ImGuizmo.SetDrawlist(ImGui.GetBackgroundDrawList());

                ImGuizmo.SetRect(wp.X, wp.Y, io.DisplaySize.X, io.DisplaySize.Y);

                ImGuizmo.SetID(222);

                ImGuizmo.AllowAxisFlip(false);

                ImGuizmo.Enable(!disabled);
                

                if(forceDisabled) { disabled = true; };

                if (mainActor.currentSkeleton != null && mainActor.currentSkeleton.isComplete)
                {
                    unsafe
                    {
                        if(modelTransform == null)
                        {
                            modelTransform = new(TransformName,
                                GetWorldToScreenPos(mainActor.GetTransform().Position), new(0, 0), circleSize, lineThickness);
                        }
                        else
                        {
                            modelTransform.Update(GetWorldToScreenPos(mainActor.GetTransform().Position), new(0), SelectedBone == null ? true : false, false, circleSize, lineThickness);
                        }


                        modelTransform.draw();

                        if (modelTransform.hovered)
                        {
                            if (!disabled)
                            {
                                if (!availableBones.Contains(modelTransform))
                                {
                                    availableBones.Add(modelTransform);
                                }
                            }

                        }
                        else
                        {
                            if (availableBones.Contains(modelTransform))
                            {
                                availableBones.Remove(modelTransform);
                            }
                        }

                        if (modelTransform.clicked)
                        {
                            if (!disabled)
                            {
                                if (!clickedBones.Contains(modelTransform))
                                {
                                    clickedBones.Add(modelTransform);
                                }
                            }
                        }
                        else
                        {
                            if (clickedBones.Contains(modelTransform))
                            {
                                clickedBones.Remove(modelTransform);
                            }
                        }

                        if (modelTransform.selected) modelTransform.drawGizmo(mainActor, viewMatrix, projectionMatrix, op, mode);
                    }

                    if(mainActor.currentSkeleton != null)
                    {
                        for(var i = 0; i < mainActor.currentSkeleton.skeletons.Count; i++)
                        {
                            var skeleton = mainActor.currentSkeleton.skeletons[i];

                            switch (i)
                            {
                                case 0:
                                    if (!showBase) continue;
                                    break;
                                case 1:
                                    if(!showFace) continue;
                                    break;
                                case 2:
                                    if(!showhair) continue;
                                    break;
                                case 3:
                                    if(!showTertiary) continue;
                                    break;
                                case 4:
                                    if (!showTertiary) continue;
                                    break;
                                default:
                                    break;
                            }

                            foreach (var bone in skeleton.boneList)
                            {
                                DrawBone(bone, SkeletonType.Main);
                            }
                        }

                        if(showWeapon)
                        {
                            if (mainActor.currentSkeleton.mhSkeletons != null)
                            {
                                foreach (var skeleton in mainActor.currentSkeleton.mhSkeletons)
                                {
                                    foreach (var bone in skeleton.boneList)
                                    {
                                        DrawBone(bone, SkeletonType.Mainhand);
                                    }
                                }
                            }

                            if (mainActor.currentSkeleton.ohSkeletons != null)
                            {
                                foreach (var skeleton in mainActor.currentSkeleton.ohSkeletons)
                                {
                                    foreach (var bone in skeleton.boneList)
                                    {
                                        DrawBone(bone, SkeletonType.Offhand);
                                    }
                                }
                            }
                        }
                    }
                }

                if (availableBones.Count > 0) ImGui.OpenPopup("boneNamesPopup");

                using (var popup = ImRaii.Popup("boneNamesPopup"))
                {
                    if (popup.Success)
                    {
                        foreach (var bone in availableBones)
                        {
                            ImGui.Text($"{bone.name}");
                        }

                        if (availableBones.Count == 0) ImGui.CloseCurrentPopup();
                    }
                }

                if (clickedBones.Count > 0)
                {
                    if (clickedBones.Count == 1)
                    {
                        if (clickedBones[0].name != TransformName)
                        {
                            var clickableBone = clickedBones[0] as ClickableBone;

                            if (clickableBone != null)
                            {
                               SelectedBone = bones[clickableBone.bone];
                            }

                        }
                        else SelectedBone = null;
                    }
                    else
                    {
                        disabled = true;
                        availableBones.Clear();
                        selectableBones = new List<Clickable>(clickedBones);
                        ImGui.SetNextFrameWantCaptureMouse(true);
                        ImGui.OpenPopup("boneSelectPopup");
                        
                    }
                }

                using (var popup = ImRaii.Popup("boneSelectPopup"))
                {
                    if (popup.Success)
                    {
                        foreach (var bone in selectableBones)
                        {
                            var selected = ImGui.Selectable(bone.name);

                            if (selected)
                            {
                                if (bone.name != TransformName)
                                {
                                    var tempSelect = bone as ClickableBone;

                                    if (tempSelect != null)
                                    {
                                        SelectedBone = bones[tempSelect.bone];
                                    }
                                    else SelectedBone = null;

                                    disabled = false;
                                }

                                ImGui.CloseCurrentPopup();
                            }
                        }
                    }
                    else
                    {
                        disabled = false;
                    }
                }

                BoneUIDraw();
            }
        }

        public static void Reset()
        {
            if(mainActor != null)
            {
                mainActor.currentSkeleton = null;
            }
            availableBones.Clear();
            bones.Clear();
            clickedBones.Clear();
            selectableBones.Clear();
            SelectedBone = null!;
        }

        private static void BoneUIDraw()
        {
            var size = new Vector2(-1, -1);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowSizeConstraints(new Vector2(360, 0), new Vector2(550, 750));

            if (ImGui.Begin("Concept Matrix: Skeleton Editor", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollWithMouse))
            {
                Transform transform = mainActor.GetTransform();

                string Name = TransformName;

                IllusioBone sBone = null;

                bool dirtyValue = false;

                if (SelectedBone != null)
                {
                    transform = SelectedBone.bone.GetTransformData();
                    Name = SelectedBone.name;
                    sBone = SelectedBone.bone;
                }

                float oldScale = ImGui.GetFont().Scale;

                BearGUI.Text($"Current Selection: {Name}", 1.1f);
                
                ImGui.Separator();

                BearGUI.Text("Tools & Settings", 1.1f);

                ImGui.Spacing();

                ImGui.BeginGroup();

                var buttonPos = ImGui.GetCursorPos();

                if (BearGUI.ImageButton("Translate", GameResourceManager.Instance.GetResourceImage("Translate.png").ImGuiHandle, new(33,33)))
                {
                    op = OPERATION.TRANSLATE;
                }

                if(op == OPERATION.TRANSLATE)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPos(buttonPos);
                    ImGui.Image(GameResourceManager.Instance.GetResourceImage("SelectSlot.png").ImGuiHandle, new(33, 33));
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Mode: Translate");
                }

                ImGui.SameLine();

                buttonPos = ImGui.GetCursorPos();

                if (BearGUI.ImageButton("Rotation", GameResourceManager.Instance.GetResourceImage("Rotate.png").ImGuiHandle, new(33, 33)))
                {
                    op = OPERATION.ROTATE;
                }

                if (op == OPERATION.ROTATE)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPos(buttonPos);
                    ImGui.Image(GameResourceManager.Instance.GetResourceImage("SelectSlot.png").ImGuiHandle, new(33, 33));
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Mode: Rotate");
                }

                ImGui.SameLine();

                buttonPos = ImGui.GetCursorPos();

                if (BearGUI.ImageButton("Scale", GameResourceManager.Instance.GetResourceImage("Scale.png").ImGuiHandle, new(33, 33)))
                {
                    op = OPERATION.SCALE;
                }

                if (op == OPERATION.SCALE)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPos(buttonPos);
                    ImGui.Image(GameResourceManager.Instance.GetResourceImage("SelectSlot.png").ImGuiHandle, new(33, 33));
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Mode: Scale");
                }

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 7);

                string modeText = mode == MODE.LOCAL ? "Local Space" : "World Space";

                if (ImGui.Button(modeText, new(115, 30)))
                {
                    mode = mode == MODE.LOCAL ? MODE.WORLD : MODE.LOCAL;
                }

                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.BeginGroup();

                ImGui.Checkbox("Disable Node Selection", ref forceDisabled);

                ImGui.SetNextItemWidth(100);
                ImGui.DragFloat("Node Size", ref circleSize, 1, 1, 25);

                ImGui.SetNextItemWidth(100);
                ImGui.DragFloat("Bone Size", ref lineThickness, 1, 0, 10);

                ImGui.EndGroup();

                if(ImGui.Checkbox("Freeze All Animations", ref freezeAnim))
                {
                    if (freezeAnim)
                    {
                        PosingManager.Instance.FreezeAnimation();
                    }
                    else
                    {
                        PosingManager.Instance.UnfreezeAnimation();
                    }
                }

                ImGui.Separator();

                BearGUI.Text("Skeleton Controller", 1.1f);

                ImGui.Text($"Position:");

                ImGui.SameLine();

                ImGui.SetCursorPosX(100);

                Vector3 pos = transform.Position;

                dirtyValue |= ImGui.DragFloat3("##transPos", ref pos, .01f);


                ImGui.Text($"Rotation:");

                ImGui.SameLine();

                ImGui.SetCursorPosX(100);

                Quaternion rot = new(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W);

                var convertedRot = rot.ToEuler();

                dirtyValue |= ImGui.DragFloat3("##transRot", ref convertedRot, .01f);

                ImGui.Text($"Scale:");

                ImGui.SameLine();

                ImGui.SetCursorPosX(100);

                Vector3 scale = transform.Scale;

                dirtyValue |= ImGui.DragFloat3("##transScale", ref scale, .01f);

                Transform finalTransform = new()
                {
                    Position = pos,
                    Rotation = convertedRot.ToQuaternion(),
                    Scale = scale,
                };

                if (dirtyValue)
                {
                    if (sBone != null)
                    {
                        sBone.illusioSkeleton.UpdateBone(finalTransform, sBone, sBone.boneType, mirror, chain);
                    }
                    else
                    {
                        if (DalamudServices.clientState.IsGPosing || mainActor.isCustom)
                        {
                            mainActor.SetOverrideTransform(finalTransform);
                        }
                    }
                }

                ImGui.Spacing();

                string mirrorTex = "Mirror Off";

                switch (mirror)
                {
                    case MirrorModes.None:
                        mirrorTex = "Mirror Off";
                        break;
                    case MirrorModes.Copy:
                        mirrorTex = "Mirror Copy";
                        break;
                    case MirrorModes.Full:
                        mirrorTex = "Mirror On";
                        break;
                    case MirrorModes.MirrorX:
                        mirrorTex = "Mirror X Only";
                        break;
                    case MirrorModes.MirrorY:
                        mirrorTex = "Mirror Y Only";
                        break;
                    case MirrorModes.MirrorZ:
                        mirrorTex = "Mirror Z Only";
                        break;
                }

                ImGui.SetCursorPosX(100);
                if (ImGui.Button(mirrorTex, new(82, 22)))
                {
                    switch (mirror)
                    {
                        case MirrorModes.None:
                            mirror = MirrorModes.Copy;
                            break;
                        case MirrorModes.Copy:
                            mirror = MirrorModes.Full;
                            break;
                        case MirrorModes.Full:
                            mirror = MirrorModes.MirrorX;
                            break;
                        case MirrorModes.MirrorX:
                            mirror = MirrorModes.MirrorY;
                            break;
                        case MirrorModes.MirrorY:
                            mirror = MirrorModes.MirrorZ;
                            break;
                        case MirrorModes.MirrorZ:
                            mirror = MirrorModes.None;
                            break;
                    }
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(186);

                string chainText = chain ? "Chain On" : "Chain Off";

                if (ImGui.Button(chainText, new(82, 22)))
                {
                    chain = !chain;
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(272);

                if (ImGui.Button("Reset Bone", new(82, 22)))
                {
                    if (sBone != null)
                    {
                        sBone.Reset();
                    }
                    else
                    {
                        if (DalamudServices.clientState.IsGPosing || mainActor.isCustom)
                        {
                            mainActor.ResetTransform();
                        }
                    }
                }

                if (ImGui.CollapsingHeader("Basic Filters"))
                {
                    var toggleStart = ImGui.GetCursorPos();

                    ImGui.Checkbox("Body", ref showBase);

                    ImGui.SetCursorPos(new(toggleStart.X + 84, toggleStart.Y));

                    ImGui.Checkbox("Face", ref showFace);

                    ImGui.SetCursorPos(new(toggleStart.X + 168, toggleStart.Y));

                    ImGui.Checkbox("Hair", ref showhair);

                    toggleStart = ImGui.GetCursorPos();

                    ImGui.Checkbox("Weapon", ref showWeapon);

                    ImGui.SetCursorPos(new(toggleStart.X + 84, toggleStart.Y));

                    ImGui.Checkbox("Tertiary", ref showTertiary);

                    ImGui.SetCursorPos(new(toggleStart.X + 168, toggleStart.Y));

                    ImGui.Checkbox("IVCS", ref showIVCS);
                }

                if (ImGui.CollapsingHeader("Advanced Filters"))
                {
                    var toggleStart = ImGui.GetCursorPos();
                    ImGui.BeginGroup();
                    BearGUI.Text("Face", 1.1f);

                    ImGui.Checkbox("Eyes", ref showEyes);
                    ImGui.Checkbox("Mouth", ref showMouth);
                    ImGui.Checkbox("Ears", ref showHead);

                    ImGui.EndGroup();

                    ImGui.SameLine();

                    //ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
                    ImGui.SetCursorPos(new(toggleStart.X + 84, toggleStart.Y - 3));

                    ImGui.BeginGroup();
                    BearGUI.Text("Body", 1.1f);
                    ImGui.Checkbox("Legs", ref showLegs);
                    ImGui.Checkbox("Torso", ref showTorso);
                    ImGui.Checkbox("Arms", ref showArms);

                    ImGui.EndGroup();

                    ImGui.SameLine();

                    ImGui.SetCursorPos(new(toggleStart.X + 168, toggleStart.Y - 3));

                    ImGui.BeginGroup();
                    BearGUI.Text("Other", 1.1f);
                    ImGui.Checkbox("Skirt", ref showSkirt);
                    ImGui.Checkbox("Tail", ref showTail);
                    ImGui.Checkbox("Sheath", ref showBuki);

                    ImGui.EndGroup();

                    ImGui.SameLine();

                    ImGui.SetCursorPos(new(toggleStart.X + 252, toggleStart.Y - 3));

                    ImGui.BeginGroup();
                    BearGUI.Text("IVCS", 1.1f);
                    ImGui.Checkbox("NSFW", ref showNSFW);
                    ImGui.Checkbox("Physics", ref showIVCSPhys);
                    ImGui.EndGroup();

                }

                if (ImGui.CollapsingHeader("Skeleton Tree"))
                {
                    if (ImGui.BeginChild("##skeletontreechild", new(350, 125)))
                    {
                        if (mainActor.currentSkeleton != null)
                            BoneTree(mainActor.currentSkeleton.skeletons[0].boneList[0]);

                        ImGui.EndChild();
                    }
                }

                ImGui.Separator();
                ImGui.SetCursorPosX(135);

                if (BearGUI.ImageButton($"Save Pose", GameResourceManager.Instance.GetResourceImage("CharaSave.png").ImGuiHandle, new(26, 22)))
                {
                    WindowsManager.Instance.fileDialogManager.SaveFileDialog("Export Pose File", ".pose", "ExportedPose", ".pose", (Confirm, FilePath) =>
                    {
                        if (Confirm)
                        {
                            var poseFile = mainActor.currentSkeleton.SaveSkeletonPose();
                            var result = JsonHandler.Serialize(poseFile);

                            File.WriteAllText(FilePath, result);
                        }
                    });
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Save Pose Data");
                }

                ImGui.SameLine();

                if (BearGUI.ImageButton($"Load Pose", GameResourceManager.Instance.GetResourceImage("CharaLoad.png").ImGuiHandle, new(26, 22)))
                {
                    WindowsManager.Instance.fileDialogManager.OpenFileDialog("Import Pose File", ".pose", (Confirm, FilePath) =>
                    {
                        if (Confirm)
                        {
                            PoseFile poseFile = new PoseFile();

                            if (Path.GetExtension(FilePath) == ".pose")
                            {
                                poseFile = JsonHandler.Deserialize<PoseFile>(File.ReadAllText(FilePath));
                            }
                            else
                            {
                                //TODO MAKE NOT UGLY
                                var file = JsonHandler.Deserialize<CMToolPoseFile>(File.ReadAllText(FilePath));
                                poseFile = file.Upgrade();
                            }

                            mainActor.currentSkeleton.ApplySkeletonPose(poseFile);
                        }
                    });
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Load Pose Data");
                }

                ImGui.SameLine();

                if (BearGUI.ImageButton($"Refresh Skeleton", GameResourceManager.Instance.GetResourceImage("CharaRefresh.png").ImGuiHandle, new(26, 22)))
                {
                    mainActor.currentSkeleton.ResetSkeleton();
                }


                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Reset Character Orientation & Pose");
                }
            }
        }

        private static void BoneTree(IllusioBone currentBone)
        {
            List<IllusioBone> children = new();

            if (currentBone.Name == "n_throw") return;

            foreach(var skeleton in mainActor.currentSkeleton.skeletons)
            {
                foreach(var bone in skeleton.boneList)
                {
                    if(skeleton.partialIDX == 0 && bone.Name == "j_ago") continue;

                    if(bone.parentIDX == 0 && bone.partialIDX != 0)
                    {
                        if (skeleton.connectedBoneIDX == currentBone.idx && bone.idx != 0)
                        {
                            children.Add(bone);
                            continue;
                        }
                    }

                    if(bone.partialIDX == currentBone.partialIDX && bone.parentIDX == currentBone.idx)
                    {
                        children.Add(bone);
                    }
                }
            }

            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnDoubleClick;

            if (children.Count == 0) flags |= ImGuiTreeNodeFlags.Leaf;

            if (currentBone.idx == 0) flags |= ImGuiTreeNodeFlags.DefaultOpen;

            string boneName = currentBone.Name;

            if (currentBone.idx == 0) boneName = TransformName;

            using (var node = ImRaii.TreeNode($"{boneName}##{currentBone.idx}{currentBone.partialIDX}", flags))
            {

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    if(currentBone.idx == 0)
                    {
                        SelectedBone = null;
                    }
                    else
                    {
                        if(bones.TryGetValue(currentBone, out var foundBone))
                        {
                            SelectedBone = foundBone;
                        }
                        
                    }
                }

                if (node.Success)
                {
                    foreach (var child in children)
                    {
                        BoneTree(child);
                    }
                }
            }
        }

        private static bool FilterBone(string name)
        {
            if (!showNSFW)
            {
                if (BoneGroups.NSFWBones.Contains(name)) return true;
            }

            if (!showEyes)
            {
                if (BoneGroups.EyeBones.Contains(name)) return true;
            }

            if (!showMouth)
            {
                if (BoneGroups.MouthBones.Contains(name)) return true;
            }

            if (!showHead)
            {
                if (BoneGroups.HeadBones.Contains(name)) return true;
            }

            if (!showTorso)
            {
                if (BoneGroups.TorsoBones.Contains(name)) return true;
            }

            if (!showArms)
            {
                if (BoneGroups.ArmBones.Contains(name)) return true;
            }

            if (!showLegs)
            {
                if (BoneGroups.LegBones.Contains(name)) return true;
            }

            if (!showSkirt)
            {
                if (BoneGroups.SkirtBones.Contains(name)) return true;
            }

            if (!showTail)
            {
                if (BoneGroups.TailBones.Contains(name)) return true;
            }

            if (!showBuki)
            {
                if (name.Contains("buki", StringComparison.OrdinalIgnoreCase)) return true;
            }

            if (!showIVCS)
            {
                if (BoneGroups.IVCSBones.Contains(name)) return true;
            }

            if (!showIVCSPhys)
            {
                if (BoneGroups.IVCSPhysBones.Contains(name)) return true;
            }

            if (!showhair)
            {
                if (name.Contains("kami", StringComparison.OrdinalIgnoreCase)) return true;
            }

            if (name.Contains("noanim", StringComparison.OrdinalIgnoreCase)) return true;

            if (name.Contains("prm_", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private static unsafe void DrawBone(IllusioBone bone, SkeletonType type)
        {
            if (FilterBone(bone.Name)) return;

            Vector2 parentPos = new(0);

            if(type == SkeletonType.Main)
            {
                if (bone.idx == 0 || bone.Name == "n_throw") return;

                if (bone.partialIDX == 0 && bone.Name == "j_ago") return;
            }

            if (mainActor.currentSkeleton != null && mainActor.currentSkeleton.GetBoneByIDX(bone.parentIDX, bone.partialIDX, type, out var parentBone))
            {
                parentPos = GetWorldToScreenPos(parentBone.GetWorldPos());
            }

            var pos = GetWorldToScreenPos(bone.GetWorldPos());

            ClickableBone boneUI = new(bone.Name, pos, bone.idx == 0 ? pos : parentPos, bone, type, circleSize, lineThickness);

            if (bones.ContainsKey(bone))
            {
                boneUI = bones[bone];

                boneUI.Update(pos, bone.idx == 0 ? pos : parentPos, SelectedBone == boneUI, disabled, circleSize, lineThickness);
            }
            else
            {
                bones.Add(bone, boneUI);
            }

            if (boneUI.selected)
            {
                switch (type)
                {
                    case SkeletonType.Main:
                        boneUI.drawGizmo(actorWorldViewMatrix, projectionMatrix, op, mode, mirror, chain);
                        break;
                    case SkeletonType.Mainhand:
                        boneUI.drawGizmo(mainhandWVM, projectionMatrix, op, mode, mirror, chain);
                        break;
                    case SkeletonType.Offhand:
                        boneUI.drawGizmo(offhandWVM, projectionMatrix, op, mode, mirror, chain);
                        break;
                }
            }

            boneUI.draw();

            if (boneUI.hovered)
            {
                if (!disabled)
                {
                    if (!availableBones.Contains(boneUI))
                    {
                        availableBones.Add(boneUI);
                    }
                }
                
            }
            else
            {
                if (availableBones.Contains(boneUI))
                {
                    availableBones.Remove(boneUI);
                }
            }

            if (boneUI.clicked)
            {
                if (!disabled)
                {
                    if (!clickedBones.Contains(boneUI))
                    {
                        clickedBones.Add(boneUI);
                    }
                }
            }
            else
            {
                if (clickedBones.Contains(boneUI))
                {
                    clickedBones.Remove(boneUI);
                }
            }
        }

        private static unsafe Vector2 GetWorldToScreenPos(Vector3 objPos)
        {
            var cam = CameraManager.Instance()->GetActiveCamera();

            Vector2 screenPos = new();

            Device* ptr = Device.Instance();
            float num = ptr->Width;
            float num2 = ptr->Height;
            Vector4 vector = Vector4.Transform(new Vector4(objPos, 1f), Matrix4x4.Multiply(cam->CameraBase.SceneCamera.ViewMatrix, XIVCamera.instance.GetCurrentCamera()->GetProjectionMatrix()));
            if (Math.Abs(vector.W) < float.Epsilon)
            {
                return screenPos = Vector2.Zero;
            }

            vector *= MathF.Abs(1f / vector.W);
            screenPos = new Vector2
            {
                X = (vector.X + 1f) * num * 0.5f,
                Y = (1f - vector.Y) * num2 * 0.5f
            };

            return screenPos;
        }
    }

    public class ClickableBone : Clickable
    {
        public IllusioBone bone;
        public SkeletonType boneType;

        public ClickableBone(string _name, Vector2 _pos, Vector2 _lineEndPos, IllusioBone bone, SkeletonType type, float _circleSize, float _lineThickness) : base(_name, _pos, _lineEndPos, _circleSize, _lineThickness)
        {
            this.name = _name;
            this.lineEndPos = _lineEndPos;
            this.pos = _pos;
            this.bone = bone;
            this.boneType = type;

            selectedColor = 0xD13131FF;

            if (IllusioVitae.configuration.UseSkeletonColors)
            {
                circleColor = 0xFF75D66D;
                
                if (_name.EndsWith("_r"))
                {
                    circleColor = 0xFF89d2dc;
                }

                if (_name.EndsWith("_l"))
                {
                    circleColor = 0xFFbcd979;
                }
            }
            else
            {
                circleColor = 0xFFCECECE;
            }
        }

        public override void Update(Vector2 _pos, Vector2 _startPos, bool _selected, bool _disabled, float _circleSize, float _lineThickness)
        {
            base.Update(_pos, _startPos, _selected, _disabled, _circleSize, _lineThickness);
        }

        public override void draw()
        {
            base.draw();
        }

        public void drawGizmo(Matrix4x4 worldViewMatrix, Matrix4x4 projectionMatrix, OPERATION op, MODE mode, MirrorModes mirror, bool chain)
        {
            var matrix = bone.GetTransformData().ToMatrix();

            if (ImGuizmo.Manipulate(
                ref worldViewMatrix.M11,
                ref projectionMatrix.M11,
                op,
                mode,
                ref matrix.M11
            ))
            {
                bone.illusioSkeleton.UpdateBone(matrix.ToTransform(), bone, boneType, mirror, chain);
            }
        }
    }

    public class Clickable
    {
        protected float circleSize = 8, lineThickness = 2;
        protected uint circleColor = 0xFF00FFC9, selectedColor = 0xFF0ECA9F;

        public string name;

        protected Vector2 pos, lineEndPos;

        public bool hovered = false, clicked = false, selected = false, disabled = false;

        public Clickable(string _name, Vector2 _pos, Vector2 _lineEndPos, float circleSize, float lineThickness)
        {
            this.name = _name;
            this.pos = _pos;
            this.lineEndPos = _lineEndPos;
            this.circleSize = circleSize;
            this.lineThickness = lineThickness;
        }

        public virtual void Update(Vector2 _pos, Vector2 _lineEndPos, bool _selected, bool _disabled, float circleSize, float lineThickness)
        {
            this.pos = _pos;
            this.lineEndPos = _lineEndPos;
            this.selected = _selected;
            this.disabled = _disabled;
            this.circleSize = circleSize;
            this.lineThickness = lineThickness;
        }

        public virtual void draw()
        {
            if (pos == Vector2.Zero) return;

            var start = new Vector2(pos.X - circleSize, pos.Y - circleSize);
            var end = new Vector2(pos.X + circleSize, pos.Y + circleSize);

            if (disabled)
            {
                clicked = false;
            }

            if (ImGui.IsMouseHoveringRect(start, end))
            {
                if (!disabled)
                {
                    ImGui.GetWindowDrawList().AddCircleFilled(pos, circleSize, selectedColor);

                    hovered = true;

                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        clicked = true;
                    }
                }
            }
            else
            {
                ImGui.GetWindowDrawList().AddCircle(pos, circleSize, 0x7F404040);

                if (!disabled)
                {
                    ImGui.GetWindowDrawList().AddCircle(pos, circleSize, circleColor);

                    hovered = false;

                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        clicked = false;
                    }
                }
            }

            if (selected)
            {
                ImGui.GetWindowDrawList().AddCircleFilled(pos, circleSize, selectedColor);
            }

            if (!disabled)
            {
                if (lineEndPos != new Vector2(0, 0) && lineThickness != 0)
                    ImGui.GetWindowDrawList().AddLine(pos, lineEndPos, circleColor, lineThickness);
            }
            else
            {
                if (lineEndPos != new Vector2(0, 0) && lineThickness != 0)
                    ImGui.GetWindowDrawList().AddLine(pos, lineEndPos, 0x7F404040, lineThickness);
            }
                
        }

        Matrix4x4 tempMatrix = Matrix4x4.Identity;

        public void drawGizmo(XIVActor actor, Matrix4x4 worldViewMatrix, Matrix4x4 projectionMatrix, OPERATION op, MODE mode)
        {
            var matrix = actor.GetTransform().ToMatrix();

            if (ImGuizmo.Manipulate(
                ref worldViewMatrix.M11,
                ref projectionMatrix.M11,
                op,
                mode,
                ref matrix.M11
            ))
            {
                if (DalamudServices.clientState.IsGPosing || actor.isCustom)
                {
                    var newTransform = actor.GetTransform().CalculateDiff(matrix.ToTransform());
                    newTransform.Position *= .3f;

                    var finalTransform = new Transform()
                    {
                        Position = actor.GetTransform().Position + newTransform.Position,
                        Rotation = Quaternion.Concatenate(actor.GetTransform().Rotation, newTransform.Rotation),
                        Scale = actor.GetTransform().Scale + newTransform.Scale,
                    };

                    actor.SetOverrideTransform(finalTransform);
                }
            }   
        }
    }
}
