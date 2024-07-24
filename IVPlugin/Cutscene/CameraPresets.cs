using IVPlugin.ActorData;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Cutscene
{
    public static class SettingPresets
    {
        public static List<CameraPresets> presets = new List<CameraPresets>()
        {
            new CameraPresets
            {
                //Hyur Midlander Male
                race = Races.Hyur,
                gender = Genders.Masculine,
                settings = new()
            },
            new CameraPresets
            {
                //Hyur Midlander Female
                race = Races.Hyur,
                gender = Genders.Feminine,
                settings = new()
                {
                    Scale = new(0.920f,0.920f, 0.920f)
                }
            },
            new CameraPresets
            {
                //Elezen male
                race = Races.Elezen,
                gender = Genders.Masculine,
                settings = new()
                {
                    Scale = new(1.13f, 1.13f, 1.13f)
                }
            },
            new CameraPresets
            {
                //Elezen Female
                race = Races.Elezen,
                gender = Genders.Feminine,
                settings = new()
                {
                    Scale = new(1.085f, 1.085f, 1.085f)
                }
            },
            new CameraPresets
            {
                //Lalafel male
                race = Races.Lalafel,
                gender = Genders.Masculine,
                settings = new()
                {
                    Scale = new(0.49f, 0.5f, 0.49f)
                }
            },
            new CameraPresets
            {
                //Lalafel female
                race = Races.Lalafel,
                gender = Genders.Feminine,
                settings = new()
                {
                    Scale = new(0.49f, 0.5f, 0.49f)
                }
            },
            new CameraPresets
            {
                //Miqote male
                race = Races.Miqote,
                gender = Genders.Masculine,
                settings = new()
                {
                    Scale = new(1f, 1f, 1f)
                }
            },
            new CameraPresets
            {
                //Miqote female
                race = Races.Miqote,
                gender = Genders.Feminine,
                settings = new()
                {
                    Scale = new(0.89f, 0.89f, 0.89f)
                }
            },
            new CameraPresets
            {
                //Roegadyn male
                race = Races.Roegadyn,
                gender = Genders.Masculine,
                settings = new()
                {
                    Scale = new( 1.275f, 1.275f,  1.275f)
                }
            },
            new CameraPresets
            {
                //Roegadyn female
                race = Races.Roegadyn,
                gender = Genders.Feminine,
                settings = new()
                {
                    Scale = new(1.190f, 1.190f, 1.190f)
                }
            },
            new CameraPresets
            {
                //AuRa male
                race = Races.AuRa,
                gender = Genders.Masculine,
                settings = new()
                {
                    Scale = new(1f, 1f, 1f)
                }
            },
            new CameraPresets
            {
                //AuRa female
                race = Races.AuRa,
                gender = Genders.Feminine,
                settings = new()
                {
                    Scale = new(0.880f, 0.880f, 0.880f)
                }
            },
            new CameraPresets
            {
                //Hrothgar male
                race = Races.Hrothgar,
                gender = Genders.Masculine,
                settings = new()
                {
                    Scale = new(1.275f,  1.275f,  1.275f)
                }
            },
            new CameraPresets
            {
                //Hrothgar Female
                race = Races.Hrothgar,
                gender = Genders.Feminine,
                settings = new()
                {
                    Scale = new(1.085f, 1.085f, 1.085f)
                }
            },
            new CameraPresets
            {
                //Viera male
                race = Races.Viera,
                gender = Genders.Masculine,
                settings = new()
                {
                    Scale = new(1f, 1f, 1f)
                }
            },
            new CameraPresets
            {
                //Viera female
                race = Races.Viera,
                gender = Genders.Feminine,
                settings = new()
                {
                    Scale = new(0.92f, 0.92f, 0.92f)
                }
            },
        };

    }

    public struct CameraPresets
    {
        public Races race;
        public Tribes tribe;
        public Genders gender;
        public CameraSettings settings;
    }

}
