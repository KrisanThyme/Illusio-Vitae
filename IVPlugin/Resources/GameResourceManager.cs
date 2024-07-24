using IVPlugin.Resources.Sheets;
using Dalamud.Interface.Internal;
using Dalamud.Plugin;
using IVPlugin.Services;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IVPlugin.ActorData;
using Dalamud.Game.ClientState.Objects.Enums;
using IVPlugin.Core;
using Dalamud.Interface.Textures.TextureWraps;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.System.File;
using IVPlugin.Resources.Structs;
using Dalamud.Hooking;
using IVPlugin.Log;
using Penumbra.String.Classes;
using Penumbra.String;
using Lumina.Data;
using Lumina;
using System.Text;
using Dalamud;
using Dalamud.Game.ClientState.Fates;
using System.Reflection.Metadata.Ecma335;

namespace IVPlugin.Resources
{
    public unsafe class GameResourceManager : IDisposable
    {
        public static GameResourceManager Instance { get; private set; } = null!;

        private readonly Dictionary<string, object> cachedDocuments = new();
        private readonly Dictionary<string, IDalamudTextureWrap> cachedImages = new();

        public readonly IReadOnlyDictionary<uint, ActionTimeline> ActionTimelines;
        public readonly IReadOnlyDictionary<uint, Emote> Emotes;
        public readonly IReadOnlyDictionary<uint, ActionTimeline> BlendEmotes;
        public readonly IReadOnlyDictionary<uint, Lumina.Excel.GeneratedSheets.Action> Actions;
        public readonly IReadOnlyDictionary<uint, Stain> Stains;
        public readonly IReadOnlyDictionary<uint, CharaMakeTypeData> CharaMakeTypes;
        public readonly IReadOnlyDictionary<uint, HairMakeTypeData> HairMakeTypes;
        public readonly IReadOnlyDictionary<uint, Item> Items;
        public readonly IReadOnlyDictionary<uint, TerritoryType> territories;
        public readonly IReadOnlyDictionary<uint, Weather> Weathers;
        public readonly IReadOnlyDictionary<uint, ENpcBase> ENpcBases;
        public readonly IReadOnlyDictionary<uint, ENpcResident> ENpcResidents;
        public readonly IReadOnlyDictionary<uint, BNpcBase> BNpcBases;
        public readonly IReadOnlyDictionary<uint, BNpcCustomize> BNpcCustomizations;
        public readonly IReadOnlyDictionary<uint, BNpcName> BNpcNames;
        public readonly IReadOnlyDictionary<uint, NpcEquip> NpcEquips;
        public readonly IReadOnlyDictionary<uint, Lumina.Excel.GeneratedSheets.Companion> Companions;
        public readonly IReadOnlyDictionary<uint, Lumina.Excel.GeneratedSheets.Ornament> Ornaments;
        public readonly IReadOnlyDictionary<uint, Mount> Mounts;
        public readonly IReadOnlyDictionary<uint, HousingFurniture> HousingFurnitures;
        public IReadOnlyDictionary<uint, ENPCExtended> ENPCNamed;
        public IReadOnlyDictionary<uint, Glasses> Glasses;
        public IReadOnlyDictionary<uint, GlassesStyle> GlassesStyle;
        public IReadOnlyDictionary<uint, ClassJob> ClassJobs;

        public readonly IReadOnlyList<IReadOnlyList<uint>> BNPCNameIndicies;
        public readonly HumanData HumanData;
        public readonly EquipmentData EquipmentData;
        public readonly List<VoiceData> VoiceData = new();

        public GameResourceManager(IDalamudPluginInterface pluginInterface)
        {
            Instance = this;

            ActionTimelines = DalamudServices.DataManager.GetExcelSheet<ActionTimeline>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            Emotes = DalamudServices.DataManager.GetExcelSheet<Emote>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            BlendEmotes = DalamudServices.DataManager.GetExcelSheet<Emote>()!.ToDictionary(x => x.RowId, x => x.ActionTimeline[4].Value).AsReadOnly();

            Actions = DalamudServices.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            Stains = DalamudServices.DataManager.GetExcelSheet<Stain>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            CharaMakeTypes = DalamudServices.DataManager.GetExcelSheet<CharaMakeTypeData>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            HairMakeTypes = DalamudServices.DataManager.GetExcelSheet<HairMakeTypeData>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            Items = DalamudServices.DataManager.GetExcelSheet<Item>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            Weathers = DalamudServices.DataManager.GetExcelSheet<Weather>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            territories = DalamudServices.DataManager.GetExcelSheet<TerritoryType>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            HumanData = new HumanData(DalamudServices.DataManager.GetFile("chara/xls/charamake/human.cmp")!.Data);

            ENpcBases = DalamudServices.DataManager.GetExcelSheet<ENpcBase>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            ENpcResidents = DalamudServices.DataManager.GetExcelSheet<ENpcResident>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            BNpcBases = DalamudServices.DataManager.GetExcelSheet<BNpcBase>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            BNpcCustomizations = DalamudServices.DataManager.GetExcelSheet<BNpcCustomize>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            BNpcNames = DalamudServices.DataManager.GetExcelSheet<BNpcName>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            NpcEquips = DalamudServices.DataManager.GetExcelSheet<NpcEquip>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            Companions = DalamudServices.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Companion>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            Ornaments = DalamudServices.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Ornament>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            Mounts = DalamudServices.DataManager.GetExcelSheet<Mount>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            HousingFurnitures = DalamudServices.DataManager.GetExcelSheet<HousingFurniture>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            Glasses = DalamudServices.DataManager.GetExcelSheet<Glasses>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            GlassesStyle = DalamudServices.DataManager.GetExcelSheet<GlassesStyle>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            ClassJobs = DalamudServices.DataManager.GetExcelSheet<ClassJob>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            BNPCNameIndicies = BNPCNames.CreateNames();

            PopulateENPCNamed();
                
            populateVoices();

            EquipmentData = new EquipmentData();
        }
        
        private void populateVoices()
        {
            var menu = CharaMakeTypes.Select(x => x.Value).ToList();

            for(var i = 0; i < menu.Count; i++)
            {
                var x = menu[i].BuildMenus();

                var y = x.GetMenuForCustomize(CustomizeIndex.Race);

                if(y != null) 
                {

                    VoiceData currentVoices = new((Races)menu[i].Race.Row, menu[i].Gender, (Tribes)menu[i].Tribe.Row, y.Voices);

                    VoiceData.Add(currentVoices);
                
                }
            }
        }

        private void PopulateENPCNamed()
        {
            Dictionary<uint, ENPCExtended> temp = new Dictionary<uint, ENPCExtended>();


            foreach (var item in Instance.ENpcBases)
            {
                ENPCExtended tempExt = new ENPCExtended();

                tempExt.Base = item.Value;
                tempExt.resident = ENpcResidents[item.Value.RowId];

                temp.Add(item.Key, tempExt);
            }

            ENPCNamed = temp.AsReadOnly();
        }
        public IDalamudTextureWrap GetResourceImage(string name)
        {
            if (cachedImages.TryGetValue(name, out var cached))
                return cached;

            using var stream = GetRawResourceStream($"Images.{name}");
            using var reader = new BinaryReader(stream);
            var imgBin = reader.ReadBytes((int)stream.Length);
            var img = DalamudServices.textureProvider.CreateFromImageAsync(imgBin).Result;
            cachedImages[name] = img;
            return img;
        }

        public byte[] GetResourceByteFile(string name)
        {
            using var stream = GetRawResourceStream($"Files.{name}");
            using var reader = new BinaryReader(stream);
            var fileBytes = reader.ReadBytes((int)stream.Length);

            return fileBytes;
        }

        public string GetResourceStringFile(string name)
        {
            using var stream = GetRawResourceStream($"Files.{name}");
            using var reader = new StreamReader(stream);
            var file = reader.ReadToEnd();

            return file;
        }

        private Stream GetRawResourceStream(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"IVPlugin.Resources.Embedded.{name}";
            var stream = assembly.GetManifestResourceStream(resourceName);
            return stream ?? throw new Exception($"Resource {name} not found.");
        }

        

        

        public void Dispose()
        {
            foreach (var img in cachedImages.Values)
                img?.Dispose();

            cachedImages?.Clear();
            cachedDocuments?.Clear();
        }
    }

    public struct ENPCExtended
    {
        public ENpcBase Base;
        public ENpcResident resident;
    }
}
