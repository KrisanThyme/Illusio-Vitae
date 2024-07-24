using IVPlugin.ActorData;
using IVPlugin.ActorData.Structs;
using IVPlugin.Log;
using IVPlugin.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Core.Files
{
    public class FFXIVChara
    {
        byte[] data;

        public CustomizeStruct customize = new();

        public byte voice = 0;

        public string comment = string.Empty;

        public FFXIVChara(byte[] data) 
        {
            this.data = data;

            ReadFile();
        }

        private void ReadFile()
        {
            using (BinaryReader br = new BinaryReader(new MemoryStream(data)))
            {
                var magic = br.ReadUInt32();

                if (magic != 0x2013FF14)
                {
                    IllusioDebug.Log("Invalid Magic", LogType.Debug);
                    return;
                }
                    
                var version = br.ReadUInt32();

                br.ReadUInt32();

                br.ReadUInt32();

                customize.Race = (Races)br.ReadByte();
                customize.Gender = (Genders)br.ReadByte();
                customize.Age = (Age)br.ReadByte();
                customize.Height = br.ReadByte();
                customize.Tribe = (Tribes)br.ReadByte();
                customize.FaceType = br.ReadByte();
                customize.HairStyle = br.ReadByte();
                customize.HighlightsEnabled = br.ReadBoolean();
                customize.SkinTone = br.ReadByte();
                customize.REyeColor = br.ReadByte();
                customize.HairColor = br.ReadByte();
                customize.HairHighlightColor = br.ReadByte();
                customize.FaceFeatures = (FacialFeatures)br.ReadByte();
                customize.FaceFeaturesColor = br.ReadByte();
                customize.Eyebrows = br.ReadByte();
                customize.LEyeColor = br.ReadByte();
                customize.EyeShape = br.ReadByte();
                customize.NoseShape = br.ReadByte();
                customize.JawShape = br.ReadByte();
                customize.LipStyle = br.ReadByte();
                customize.LipColor = br.ReadByte();
                customize.RaceFeatureSize = br.ReadByte();
                customize.RaceFeatureType = br.ReadByte();
                customize.BustSize = br.ReadByte();
                customize.Facepaint = br.ReadByte();
                customize.FacePaintColor = br.ReadByte();
                voice = br.ReadByte();
                br.ReadByte();
                br.ReadUInt32();

                comment = Encoding.Default.GetString(br.ReadBytes(40));
            }
        }
    }
}
