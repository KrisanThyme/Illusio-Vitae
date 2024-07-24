using FFXIVClientStructs.FFXIV.Client.Game.Character;
using IVPlugin.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lumina.Excel.GeneratedSheets;
using IVPlugin.Core.Extentions;

namespace IVPlugin.Resources
{
    public class EquipmentData
    {
        private readonly MultiValueDictionary<ulong, ModelInfo> _modelLookupTable;
        private readonly List<ModelInfo> _modelsList;

        public EquipmentData()
        {
            _modelLookupTable = new();
            _modelsList = [];


            // From Game
            var items = GameResourceManager.Instance.Items.Values;
            foreach (var item in items)
            {
                var slots = item.EquipSlotCategory.Value?.GetEquipSlots() ?? ActorEquipSlot.None;
                if (slots != ActorEquipSlot.None)
                {
                    var modelInfo = new ModelInfo(item.ModelMain, item.RowId, item.Name, item.Icon, slots, item.ClassJobCategory.Value, item);
                    AddModel(modelInfo);

                    if (item.ModelSub != 0)
                    {
                        modelInfo = new ModelInfo(item.ModelSub, item.RowId, item.Name, item.Icon, ActorEquipSlot.OffHand, item.ClassJobCategory.Value, item);
                        AddModel(modelInfo);
                    }
                }
            }

            // Special
            var none = new ModelInfo(0, 0, "None", 0, ActorEquipSlot.All, new(), null);
            AddModel(none);
        }

        public ModelInfo? GetModelById(ulong modelId, ActorEquipSlot slot)
        {
            if (_modelLookupTable.TryGetValues(modelId & 0x00FFFFFFFFFFFFFF, out var values))
                return values.FirstOrDefault(x => x != null && (x.Slots & slot) != 0, null);

            return null;
        }
        public ModelInfo? GetModelById(WeaponModelId modelId, ActorEquipSlot slot) => GetModelById(modelId.Value, slot);
        public ModelInfo? GetModelById(EquipmentModelId modelId, ActorEquipSlot slot) => GetModelById((ulong)modelId.Value & 0x00FFFFFF, slot);

        public IEnumerable<ModelInfo> GetEquippableInSlots(ActorEquipSlot slots)
        {
            List<ModelInfo> models = [];
            foreach (var model in _modelsList)
            {
                if (model.Slots.HasFlag(slots))
                    models.Add(model);
            }
            return models;
        }

        public IEnumerable<ModelInfo> GetAllGear() => _modelsList;

        private void AddModel(ModelInfo info)
        {
            _modelsList.Add(info);
            _modelLookupTable.Add(info.ModelId, info);
        }

        public record class ModelInfo(ulong ModelId, uint ItemId, string Name, uint Icon, ActorEquipSlot Slots, ClassJobCategory classJob ,Item? Item);
    }

    [Flags]
    public enum ActorEquipSlot
    {
        None = 0,
        MainHand = 1 << 0,
        OffHand = 1 << 1,
        Prop = 1 << 2,
        Head = 1 << 3,
        Body = 1 << 4,
        Hands = 1 << 5,
        Legs = 1 << 6,
        Feet = 1 << 7,
        Ears = 1 << 8,
        Neck = 1 << 9,
        Wrists = 1 << 10,
        RightRing = 1 << 11,
        LeftRing = 1 << 12,

        Weapons = MainHand | OffHand | Prop,
        Armor = Head | Body | Hands | Legs | Feet,
        Accessories = Ears | Neck | Wrists | RightRing | LeftRing,
        AllButWeapons = Armor | Accessories,
        All = MainHand | OffHand | Prop | Head | Body | Hands | Legs | Feet | Ears | Neck | Wrists | RightRing | LeftRing
    }
}
