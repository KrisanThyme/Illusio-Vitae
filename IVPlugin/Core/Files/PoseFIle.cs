using FFXIVClientStructs.FFXIV.Client.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Core.Files
{
    [Serializable]
    public class PoseFile
    {
        public string FileExtension { get; set; } = ".pose";
        public string TypeName { get; set; } = "IllusioVitae Pose";

        public const int CurrentVersion = 2;
        public int FileVersion { get; set; } = 2;

        public Bone ModelDifference { get; set; } = new();

        public Dictionary<string, Bone> Bones { get; set; } = [];
        public Dictionary<string, Bone> MainHand { get; set; } = [];
        public Dictionary<string, Bone> OffHand { get; set; } = [];

        public class Bone
        {
            public Vector3 Position { get; set; }
            public Quaternion Rotation { get; set; }
            public Vector3 Scale { get; set; }

            public static implicit operator Transform(Bone bone)
            {
                return new Transform()
                {
                    Position = bone.Position,
                    Rotation = bone.Rotation,
                    Scale = bone.Scale
                };
            }

            public static implicit operator Bone(Transform bone)
            {
                return new Bone()
                {
                    Position = bone.Position,
                    Rotation = bone.Rotation,
                    Scale = bone.Scale
                };
            }
        }
    }
}
