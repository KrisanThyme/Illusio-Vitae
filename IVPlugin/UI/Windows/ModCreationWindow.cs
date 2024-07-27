using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using IVPlugin.ActorData;
using IVPlugin.Mods;
using IVPlugin.Mods.Structs;
using IVPlugin.Resources;
using IVPlugin.Services;
using IVPlugin.UI.Helpers;
using IVPlugin.UI.Windows.Tabs;
using Lumina;
using Microsoft.VisualBasic;
using Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.UI.Windows
{
    public static class ModCreationWindow
    {
        public static bool IsOpen = false;
        public static void Show() => IsOpen = true;
        public static void Toggle() => IsOpen = !IsOpen;

        public static string ModName = string.Empty, ModAuthor = string.Empty, CamPath = string.Empty;
        public static ModCatagory selectedCatagory = ModCatagory.Global;

        public static int emoteCount = 0;

        public static ModSharedResourceTab sharedResources = new();

        public static List<ModEmoteTab> emotes = new() { new() };

        public static void Draw()
        {
            if (!IsOpen) return;

            var size = new Vector2(400, 200);
            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowSizeConstraints(new(100, 100), new Vector2(900, 700));

            if (ImGui.Begin($"Mod Creation", ref IsOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ShowMeta();
                ShowEmotes();
            }
        }

        private static void ShowMeta()
        {
            ImGui.BeginGroup();
            ImGui.Text("Mod Name:");
            ImGui.SameLine(90);
            ImGui.SetNextItemWidth(100);
            ImGui.InputText("##ModNameInput", ref ModName, 100);
            ImGui.Spacing();
            ImGui.Text("Mod Author:");
            ImGui.SameLine(90);
            ImGui.SetNextItemWidth(100);
            ImGui.InputText("##ModAuthorInput", ref ModName, 200);
            ImGui.Spacing();
            ImGui.Text("Camera File:");
            ImGui.SameLine(90);
            ImGui.SetNextItemWidth(100);
            ImGui.InputText("##CameraFileInput", ref CamPath, 5000);
            ImGui.SameLine();
            if (ImGui.Button("Browse##.xcpbrowse"))
            {
                WindowsManager.Instance.fileDialogManager.OpenFileDialog("Import Camera File", ".xcp", (Confirm, FilePath) =>
                {
                    if (!Confirm) return;

                    CamPath = FilePath;
                });
            }
            ImGui.EndGroup();

            ImGui.SameLine(300);

            var currentCatagory = Enum.GetName<ModCatagory>(selectedCatagory);

            ImGui.SetNextItemWidth(100);

            using (var combo = ImRaii.Combo("##ModType", currentCatagory))
            {
                if (combo.Success)
                {
                    var catagories = Enum.GetNames(typeof(ModCatagory));

                    foreach (var catagory in catagories)
                    {
                        if (ImGui.Selectable(catagory, catagory == currentCatagory))
                        {
                            selectedCatagory = Enum.Parse<ModCatagory>(catagory);
                        }
                    }
                }
            }

            ImGui.SameLine();

            BearGUI.FontText(FontAwesomeIcon.QuestionCircle.ToIconString(), 1.25f);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Catagory Sets the type of emote it is. Most things are global for example but would \n set it to Male if you only support male animations. This has an effect on the modlist within the plugin.");
            }

            if (ImGui.Button("Import pmp file"))
            {
                ParsePMP();
            }
        }

        private static void ShowEmotes()
        {
            if(ImGui.BeginTabBar("Resource List"))
            {
                if (ImGui.BeginTabItem("Shared Resources"))
                {
                    sharedResources.Draw();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Emotes"))
                {
                    if (ImGui.Button("Add Emote"))
                    {
                        emotes.Add(new());
                    }

                    if (ImGui.BeginTabBar("Emote List"))
                    {
                        for(var i = 0; i < emotes.Count; i++)
                        {
                            var currentEmote = emotes[i];

                            if (currentEmote == null) currentEmote = new();

                            if(ImGui.BeginTabItem($"Emote {i}"))
                            {
                                currentEmote.Draw();

                                ImGui.EndTabItem();
                            }
                        }
                        ImGui.EndTabBar();
                    }

                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("Tracklist Creation"))
                {
                    TracklistGenerator.Draw();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }
    
        private static void ParsePMP()
        {
            WindowsManager.Instance.fileDialogManager.OpenFileDialog("PMP To Convert", ".pmp", (Confirm, FilePath) =>
            {
                if (!Confirm) return;

                var FinalPath = Path.Combine(DalamudServices.PluginInterface.ConfigDirectory.FullName, "PMP");

                if (File.Exists(FinalPath)) 
                {
                    Directory.Delete(FinalPath, true);
                }

                var TempPmp = Directory.CreateDirectory(FinalPath);

                using(var zip = new ZipArchive(File.Open(FilePath, FileMode.Open)))
                {
                    zip.ExtractToDirectory(FinalPath);
                }

                var MetaFile = Path.Combine(FinalPath, "meta.json");

                if (File.Exists(MetaFile))
                {

                }
            });
        }
    }

    public class ModSharedResourceTab
    {
        public List<DataPaths> paths = new List<DataPaths>();

        public void Draw()
        {
            BearGUI.FontText(FontAwesomeIcon.QuestionCircle.ToIconString(), 1.25f);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("This tab is for files that are shared between emote.");
            }

            ImGui.Spacing();

            if(ImGui.Button("Add Data path"))
            {
                paths.Add(new());
            }

            using (var dataPaths = ImRaii.Child("##SharedDataPaths", new(0)))
            {
                if (dataPaths.Success)
                {
                    var tempList = paths.ToList();

                    for (var i = 0; i < tempList.Count; i++)
                    {
                        tempList[i].Draw(i, null, this);
                    }
                }
            }

        }
    }

    public class ModEmoteTab
    {
        public string Command = string.Empty, tracklistPath = string.Empty, EmoteSearch = string.Empty;
        public int animID = 0;
        public EmoteType currentType = EmoteType.Full;
        public bool isLooping = false, hideWeapon = false;

        public List<DataPaths> paths = new List<DataPaths>();
        public List<VFXData> vFXDatas = new List<VFXData>();

        private string[] animationTypes = ["Base", "Startup", "Floor", "Sitting", "Blend", "Other", "Adjusted"];
        public void Draw()
        {
            using (ImRaii.Disabled(ModCreationWindow.emotes[0] == this))
            {
                if (ImGui.Button("Remove Emote"))
                {
                    ModCreationWindow.emotes.Remove(this);
                }
            }

            ImGui.Spacing();

            ImGui.BeginGroup();
            ImGui.Text("Emote Type:");
            ImGui.SameLine(120);
            var currentModType = Enum.GetName(currentType);
            ImGui.SetNextItemWidth(100);
            using (var combo = ImRaii.Combo("##EmoteType", currentModType))
            {
                if (combo.Success)
                {
                    var types = Enum.GetNames(typeof(EmoteType));

                    foreach (var type in types)
                    {
                        if (ImGui.Selectable(type, type == currentModType))
                        {
                            currentType = Enum.Parse<EmoteType>(type);
                        }
                    }
                }
            }
            ImGui.Text("Emote Command:");
            ImGui.SameLine(120);
            ImGui.SetNextItemWidth(100);
            ImGui.InputText("##EmoteCommandInput", ref Command, 100);
            ImGui.Spacing();
            ImGui.Text("Animation ID:");
            ImGui.SameLine(120);
            ImGui.SetNextItemWidth(70);
            ImGui.InputInt("##ReplacedAnimID", ref animID, 0);
            ImGui.SameLine();
            if (BearGUI.FontButton("emotesearch", FontAwesomeIcon.SearchLocation.ToIconString()))
            {
                ImGui.OpenPopup("EmoteSearch");
            }
            ImGui.Spacing();
            ImGui.Text("Should Loop");
            ImGui.SameLine(120);
            ImGui.SetNextItemWidth(100);
            ImGui.Checkbox("##LoopEmote", ref isLooping);
            ImGui.Spacing();
            ImGui.Text("Hide Weapons");
            ImGui.SameLine(120);
            ImGui.SetNextItemWidth(100);
            ImGui.Checkbox("##HideWeapon", ref hideWeapon);
            ImGui.Spacing();
            ImGui.Text("Tracklist:");
            ImGui.SameLine(120);
            ImGui.SetNextItemWidth(100);
            ImGui.InputText("##TracklistInput", ref tracklistPath, 500);
            ImGui.SameLine();
            if (ImGui.Button("Browse##.ivtlfile"))
            {
                WindowsManager.Instance.fileDialogManager.OpenFileDialog("Import Tracklist File", ".ivtl", (Confirm, FilePath) =>
                {
                    if (!Confirm) return;

                    tracklistPath = FilePath;
                });
            }
            ImGui.Spacing();

            ImGui.EndGroup();

            ImGui.SameLine(300);

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 25);

            using (var popup = ImRaii.Popup("EmoteSearch"))
            {
                if (popup.Success)
                {
                    ImGui.Text("Search:");
                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(150);
                    ImGui.InputText("##EmoteSearch", ref EmoteSearch, 1000);

                    var emoteList = GameResourceManager.Instance.Emotes.Select(x => x.Value).Where(x => (x.Name.RawString.Contains(EmoteSearch, StringComparison.OrdinalIgnoreCase)) && x.Name != "").ToList();

                    using (var listbox = ImRaii.ListBox($"###listbox", new(250, 200)))
                    {
                        if (listbox.Success)
                        {
                            var i = 0;
                            foreach (var emote in emoteList)
                            {
                                for (var x = 0; x < 7; x++)
                                {
                                    var currentEmote = emote.ActionTimeline[x];

                                    if (currentEmote.Value == null) continue;

                                    if (currentEmote.Value.RowId == 0) continue;

                                    i++;
                                    using (ImRaii.PushId(i))
                                    {
                                        var startPos = ImGui.GetCursorPos();

                                        var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                        var endPos = ImGui.GetCursorPos();

                                        if (ImGui.IsItemVisible())
                                        {
                                            var icon = DalamudServices.textureProvider.GetFromGameIcon(new(emote.Icon)).GetWrapOrDefault();

                                            if (icon == null) continue;

                                            ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                            ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                            ImGui.SameLine();

                                            ImGui.BeginGroup();

                                            ImGui.Text(emote.Name.RawString);

                                            ImGui.Text(emote.Name.RawString + $" ({animationTypes[x]})");

                                            ImGui.EndGroup();

                                            ImGui.SetCursorPos(endPos);

                                            if (selected)
                                            {
                                                animID = (int)currentEmote.Value.RowId;
                                                ImGui.CloseCurrentPopup();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (var child = ImRaii.Child("##papvfxData", new(0), true))
            {
                if (child.Success)
                {
                    if (ImGui.BeginTabBar("papvfxData"))
                    {
                        if(ImGui.BeginTabItem("Pap files"))
                        {
                            if(ImGui.Button("New Path"))
                            {
                                paths.Add(new());
                            }
                            using(var dataPaths = ImRaii.Child("##DataPaths", new(0)))
                            {
                                if(dataPaths.Success)
                                {
                                    var tempList = paths.ToList();

                                    for(var i = 0; i < tempList.Count; i++) 
                                    {
                                        tempList[i].Draw(i, this);
                                    }
                                }
                            }
                            ImGui.EndTabItem();
                        }

                        if(ImGui.BeginTabItem("Persistant VFX Files"))
                        {
                            if(ImGui.Button("Add VFX Data"))
                            {
                                vFXDatas.Add(new());
                            }

                            if (ImGui.BeginTabBar("VFXData"))
                            {
                                var tempList = vFXDatas.ToList();

                                for (var i = 0; i < tempList.Count; i++)
                                {
                                    if (ImGui.BeginTabItem($"VFXData{i}"))
                                    {
                                        tempList[i].Draw(this);
                                        ImGui.EndTabItem();
                                    }
                                }

                                ImGui.EndTabBar();
                            }

                            ImGui.EndTabItem();
                        }
                        ImGui.EndTabBar();
                    }
                }
            }
        }
    }

    public class DataPaths
    {
        public string GamePath = string.Empty;
        public string LocalPath = string.Empty;
        public RaceCodes validRaces = RaceCodes.all;
        public void Draw(int IDX, ModEmoteTab modemote = null, ModSharedResourceTab sharedEmote = null)
        {
            using (ImRaii.PushId(IDX))
            {
                ImGui.BeginGroup();
                ImGui.Text("Game Path:");
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(250);
                ImGui.InputText("##GamePathInput", ref GamePath, 500);
                ImGui.SameLine();
                if (ImGui.Button("Valid Races"))
                {
                    ImGui.OpenPopup("RaceSelection");
                }
                ImGui.Text("Local Path:");
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(250);
                ImGui.InputText("##LocalpathInput", ref LocalPath, 500);
                ImGui.SameLine();
                if (ImGui.Button("Browse##LocalPath"))
                {
                    WindowsManager.Instance.fileDialogManager.OpenFileDialog("Square File", ".*", (Confirm, FilePath) =>
                    {
                        if (!Confirm) return;

                        LocalPath = FilePath;
                    });
                }
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 15);

                if (ImGui.Button("Remove"))
                {
                    if (modemote != null)
                    {
                        modemote.paths.Remove(this);
                    }

                    if (sharedEmote != null)
                    {
                        sharedEmote.paths.Remove(this);
                    }
                }

                using(var popup = ImRaii.Popup("RaceSelection"))
                {
                    if (popup.Success) 
                    {
                        var raceCodes = Enum.GetNames(typeof(RaceCodes));

                        for(var i = 0; i < raceCodes.Length - 1; i++)
                        {
                            if(i != 0 && i % 3 != 0)
                            {
                                ImGui.SameLine();
                            }

                            RaceCodes raceCode = Enum.Parse<RaceCodes>(raceCodes[i]);


                            var tempRaces = validRaces;

                            var result = tempRaces.HasFlag(raceCode);

                            if (ImGui.Checkbox(raceCodes[i], ref result))
                            {
                                if (result) tempRaces |= Enum.Parse<RaceCodes>(raceCodes[i]);
                                else tempRaces ^= Enum.Parse<RaceCodes>(raceCodes[i]); 

                                validRaces = tempRaces;
                            }
                        }

                        if (ImGui.Button("Add All Races"))
                        {
                            validRaces = RaceCodes.all;
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Remove All Races"))
                        {
                            validRaces = 0;
                        }
                    }
                }
            }
            
        }
    }

    public class VFXData
    {
        public VFXType vfxType;
        public RaceCodes validRaces = RaceCodes.all;
        public List<VFXPath> vfxPaths = new List<VFXPath>() { new() };

        public void Draw(ModEmoteTab tab)
        {
            if(ImGui.Button("Remove VFX"))
            {
                tab.vFXDatas.Remove(this);
            }

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.Text("VFX Type:");
            ImGui.SameLine();
            var currentVFXType = Enum.GetName(vfxType);
            ImGui.SetNextItemWidth(100);
            using (var combo = ImRaii.Combo("##VFXType", currentVFXType))
            {
                if (combo.Success)
                {
                    var types = Enum.GetNames(typeof(VFXType));

                    foreach (var type in types)
                    {
                        if (ImGui.Selectable(type, type == currentVFXType))
                        {
                            vfxType = Enum.Parse<VFXType>(type);
                        }
                    }
                }
            }

            ImGui.SameLine();

            if(ImGui.Button("Valid Races"))
            {
                ImGui.OpenPopup("RaceSelection");
            }

            using (var popup = ImRaii.Popup("RaceSelection"))
            {
                if (popup.Success)
                {
                    var raceCodes = Enum.GetNames(typeof(RaceCodes));

                    for (var i = 0; i < raceCodes.Length - 1; i++)
                    {
                        if (i != 0 && i % 3 != 0)
                        {
                            ImGui.SameLine();
                        }

                        RaceCodes raceCode = Enum.Parse<RaceCodes>(raceCodes[i]);


                        var tempRaces = validRaces;

                        var result = tempRaces.HasFlag(raceCode);

                        if (ImGui.Checkbox(raceCodes[i], ref result))
                        {
                            if (result) tempRaces |= Enum.Parse<RaceCodes>(raceCodes[i]);
                            else tempRaces ^= Enum.Parse<RaceCodes>(raceCodes[i]);

                            validRaces = tempRaces;
                        }
                    }

                    if (ImGui.Button("Add All Races"))
                    {
                        validRaces = RaceCodes.all;
                    }

                    ImGui.SameLine();

                    if(ImGui.Button("Remove All Races"))
                    {
                        validRaces = 0;
                    }
                }
            }

            if (ImGui.Button("New VFX Path"))
            {
                vfxPaths.Add(new());
            }
            using (var vfxDataPaths = ImRaii.Child("##VFXDataPaths", new(0)))
            {
                if (vfxDataPaths.Success)
                {
                    var tempList = vfxPaths.ToList();

                    for (var i = 0; i < tempList.Count; i++)
                    {
                        tempList[i].Draw(i, this);
                    }
                }
            }
        }
    }

    public class VFXPath
    {
        public string GamePath = string.Empty;
        public string LocalPath = string.Empty;
        public void Draw(int idx, VFXData data)
        {
            using (ImRaii.PushId(idx))
            {
                ImGui.BeginGroup();
                ImGui.Text("Game Path:");
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(250);
                ImGui.InputText("##GamePathInput", ref GamePath, 500);
                ImGui.Text("Local Path:");
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(250);
                ImGui.InputText("##LocalpathInput", ref LocalPath, 500);
                ImGui.SameLine();
                if (ImGui.Button("Browse##LocalPath"))
                {
                    WindowsManager.Instance.fileDialogManager.OpenFileDialog("Square File", ".*", (Confirm, FilePath) =>
                    {
                        if (!Confirm) return;

                        LocalPath = FilePath;
                    });
                }
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 15);

                if (ImGui.Button("Remove"))
                {
                    data.vfxPaths.Remove(this);
                }
            }
        }
            
    }

    public static class TracklistGenerator
    {
        public static IVTrack[] tracks = new IVTrack[0];
        public static int selectedTrackIndex = -1;
        public static void Draw()
        {
            BearGUI.FontText(FontAwesomeIcon.QuestionCircle.ToIconString(), 1.25f);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("This tab is the creation of a IV tracklist. With this you will be able to call certain functions\n of the plugin during an animation");
            }

            ImGui.BeginGroup();

            using (var listBox = ImRaii.ListBox("##Tracks", new(300, 0)))
            {
                if (listBox.Success)
                {
                    for(var i = 0; i < tracks.Length; i++)
                    {
                        var track = tracks[i];

                        if(ImGui.Selectable($"Track {i}##{i}", i == selectedTrackIndex))
                        {
                            selectedTrackIndex = i;
                        }
                    }
                }
            }

            if(ImGui.Button("Add Track"))
            {
               tracks = tracks.Append(new()).ToArray();
            }

            ImGui.SameLine();

            if(ImGui.Button("Remove Selected Track"))
            {
                var trackTodelete = selectedTrackIndex;

                if(selectedTrackIndex != 0)
                    selectedTrackIndex--;
                
                var x =  tracks.ToList();

                x.RemoveAt(trackTodelete);

                tracks = x.ToArray();

            }

            ImGui.EndGroup();

            ImGui.SameLine(400);

            ImGui.BeginGroup();

            var currentTrack = tracks[selectedTrackIndex];

            ImGui.Text("Track Type");

            ImGui.SameLine(90);

            var selectedTrackType = Enum.GetName(currentTrack.Type);

            ImGui.SetNextItemWidth(200);
            using (var combo = ImRaii.Combo("##tracklistType", selectedTrackType))
            {
                if (combo.Success)
                {
                    var types = Enum.GetNames(typeof(TrackType));

                    foreach (var type in types)
                    {
                        if (ImGui.Selectable(type, type == selectedTrackType))
                        {
                            tracks[selectedTrackIndex].Type = Enum.Parse<TrackType>(type);
                        }
                    }
                }
            }

            ImGui.Text("Frame");
            ImGui.SameLine(90);
            int setFrame = (int)currentTrack.Frame;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("##trackFrame", ref setFrame, 0))
            {
                tracks[selectedTrackIndex].Frame = (uint)setFrame;
            };

            string value1Text = "Unavailable";
            string value2Text = "Unavailable";
            bool value1Disabled = true;
            bool value2Disabled = true;

            switch (currentTrack.Type)
            {
                case TrackType.Expression:
                    value1Text = "Emote ID";
                    value1Disabled = false;
                    value2Disabled = true;
                    break;
                case TrackType.Transparency:
                    value1Text = "Value";
                    value1Disabled = false;
                    value2Disabled = true;
                    break;
                case TrackType.FadeIn:
                    value1Text = "FadeIn Time";
                    value1Disabled = false;
                    value2Disabled = true;
                    break;
                case TrackType.FadeOut:
                    value1Text = "FadeOut Time";
                    value1Disabled = false;
                    value2Disabled = true;
                    break;
                case TrackType.Scale:
                    value1Text = "Scale Value";
                    value1Disabled = false;
                    value2Disabled = true;
                    break;
                case TrackType.Outfit:
                    value2Text = ".chara Path";
                    value1Disabled = true;
                    value2Disabled = false;
                    break;
                case TrackType.ChangeTime:
                    value1Text = "Time Of Day";
                    value1Disabled = false;
                    value2Disabled = true;
                    break;
                case TrackType.ChangeMonth:
                    value1Text = "Day of Month";
                    value1Disabled = false;
                    value2Disabled = true;
                    break;
                case TrackType.ChangeSkybox:
                    value1Text = "Skybox ID";
                    value1Disabled = false;
                    value2Disabled = true;
                    break;
            }



            using (ImRaii.Disabled(value1Disabled))
            {
                ImGui.Text(value1Text);
                ImGui.SameLine(90);
                float setValue1 = currentTrack.Value ?? 0;
                ImGui.SetNextItemWidth(200);
                if (ImGui.InputFloat("##floatValue", ref setValue1, 0))
                {
                    tracks[selectedTrackIndex].Value = setValue1;
                }
            }

            using (ImRaii.Disabled(value2Disabled))
            {
                ImGui.Text(value2Text);
                ImGui.SameLine(90);
                string setValue2 = currentTrack.sValue ?? string.Empty;
                ImGui.SetNextItemWidth(200);
                if (ImGui.InputText("##stringValue", ref setValue2, 1000))
                {
                    tracks[selectedTrackIndex].sValue = setValue2;
                }
            }
            
            

            ImGui.EndGroup();
        }
    }


}
