using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientObjectManager = FFXIVClientStructs.FFXIV.Client.Game.Object.ClientObjectManager;
using IVPlugin.Services;
using IVPlugin.Core.Extentions;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Common.Lua;
using IVPlugin.Core.Files;
using IVPlugin.Actors.Structs;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using IVPlugin.Log;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace IVPlugin.ActorData
{
    public class CustomActor
    {
        public IGameObject? customGO;
        public uint characterID;
        public string customName = "";
        public CharaFile appearanceData = null;
        public ShaderParams sParams;

        public CustomActor(int id, CharaFile data = null, string newName = "", bool spawnWithCompanion = false)
        {
            if (newName != "")
                customName = newName;
            else
                customName = $"Illusio Actor";
                //customName = $"Illusio {GetColumnName(id)}-actor";

            appearanceData = data;

            CreateNewActor(spawnWithCompanion);

            if(customGO == null)
            {
                IllusioDebug.Log("Unable to create custom actor", LogType.Debug);
            }
        }

        public void CreateNewActor(bool spawnWithCompanion = false)
        {
            ICharacter newChar;

            unsafe
            {
                var comInstance = ClientObjectManager.Instance();

                characterID = comInstance->CreateBattleCharacter(param: spawnWithCompanion ? (byte)1 : (byte)0);

                // Actor Failed to Generate
                if (characterID == 0xffffffff) return;

                var customObject = comInstance->GetObjectByIndex((ushort)characterID);

                if (customObject == null) return;

                var ef = EventFramework.Instance();
                if (ef == null)
                    return;

                var newPlayer = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)customObject;

                ef->EventSceneModule.EventGPoseController.AddCharacterToGPose(newPlayer);

                customGO = DalamudServices.objectTables.CreateObjectReference((nint)customObject);

                for(var i = 0; i < customName.Length; i++)
                {
                    customObject->Name[i] = (byte)customName[i];
                }

                customObject->Name[customName.Length] = 0;

                var customGOStruct = customGO.Base();
                var customChar = (ICharacter)customGO;
                var customCharStruct = customChar.Base();

                XIVActor player = ActorManager.Instance.playerActor;

                if (DalamudServices.clientState.IsGPosing) player = ActorManager.Instance.mainGposeActor;

                customGOStruct->ObjectKind = FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectKind.Pc;

                var rot = ExtentionMethods.ToEuler(player.GetTransform().Rotation);
                var rotX = rot.X * ExtentionMethods.DegreesToRadians;


                customGOStruct->DefaultPosition = player.actorObject.Position;
                customGOStruct->DefaultRotation = rotX;
                customGOStruct->Position = player.actorObject.Position;
                customGOStruct->Rotation = rotX;

                customCharStruct->DrawData.CustomizeData.Race = 1;
                customCharStruct->DrawData.CustomizeData.Tribe = 1;
                customCharStruct->DrawData.CustomizeData.Sex = 0;
                customCharStruct->DrawData.CustomizeData.BodyType = 1;
                customCharStruct->DrawData.CustomizeData.Height = 50;
                customCharStruct->DrawData.CustomizeData.Face = 5;
                customCharStruct->DrawData.CustomizeData.Hairstyle = 1;

                customCharStruct->DrawData.CustomizeData.SkinColor = 10;
                customCharStruct->DrawData.CustomizeData.HairColor = 70;
                customCharStruct->DrawData.CustomizeData.EyeColorLeft = 145;
                customCharStruct->DrawData.CustomizeData.EyeColorRight = 145;
                customCharStruct->DrawData.CustomizeData.TattooColor = 1;
                customCharStruct->DrawData.CustomizeData.FacePaintColor = 1;

                customCharStruct->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head) = new() { Id = 84 };
                customCharStruct->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body) = new() { Id = 84 };
                customCharStruct->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Legs) = new() { Id = 84 };
                customCharStruct->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Feet) = new() { Id = 84 };

                if (appearanceData != null)
                {
                    appearanceData.ApplyRaw(customCharStruct, CharaFile.SaveModes.All, out sParams);
                }

                if(!DalamudServices.clientState.IsGPosing && !IllusioVitae.InDebug())
                {
                    customGOStruct->TargetableStatus &= ~FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectTargetableFlags.IsTargetable;
                }

                customCharStruct->DrawData.HideWeapons(true);

                WaitForDraw(customGOStruct);
            }
        }

        public static string GetColumnName(int index)
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var value = "";

            if (index >= letters.Length)
                value += letters[index / letters.Length - 1];

            value += letters[index % letters.Length];

            return value;
        }
        
        private unsafe void WaitForDraw(FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* obj)
        {
           DalamudServices.framework.RunOnTick(() => 
           {
               if (obj->IsReadyToDraw())
               {
                   obj->EnableDraw();

                   SetShaderParams(obj);
               }
               else
               {
                   WaitForDraw(obj);
               }
           });
        }

        private unsafe void SetShaderParams(FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* obj)
        {
            var charaBase = (ExtendedCharStruct*)obj->DrawObject;

            if (charaBase->CharacterBase.GetModelType() != CharacterBase.ModelType.Human) return;

            var human = (ExtendedHumanStruct*)charaBase;

            ShaderParams test = new();

            if (sParams.Equals(test)) return;

            human->Shaders->Params->FeatureColor = sParams.FeatureColor;
            human->Shaders->Params->HairColor = sParams.HairColor;
            human->Shaders->Params->HairGloss = sParams.HairGloss;
            human->Shaders->Params->HairHighlight = sParams.HairHighlight;
            human->Shaders->Params->LeftEyeColor = sParams.LeftEyeColor;
            human->Shaders->Params->RightEyeColor = sParams.RightEyeColor;
            human->Shaders->Params->SkinColor = sParams.SkinColor;
            human->Shaders->Params->SkinGloss = sParams.SkinGloss;
            human->Shaders->Params->MouthColor = sParams.MouthColor;
            human->Shaders->Params->MuscleTone = sParams.MuscleTone;
        }

        public void DestroyActor()
        {
            unsafe
            {
                var comInstance = ClientObjectManager.Instance();

                comInstance->DeleteObjectByIndex((ushort)characterID, 0);
            }
            
        }
    }

    public struct CustomActorInfo
    {
        public string Name { get; set; }
        public string charaPath { get; set; }

        public CustomActorInfo(string name, string charaPath)
        {
            this.Name = name;
            this.charaPath = charaPath;
        }
    }
}
