using IVPlugin.VFX.Struct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.VFX
{
    public unsafe class VFXData
    {
        public bool IsStatic = false;
        public string Path;
        public VfxStruct* Vfx;
        public bool Removed = false;
        public DateTime RemovedTime;
    }
}
