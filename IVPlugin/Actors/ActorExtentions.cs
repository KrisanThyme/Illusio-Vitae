using FFXIVClientStructs.FFXIV.Client.Game.Character;
using IVPlugin.ActorData;
using IVPlugin.ActorData.Structs;
using IVPlugin.Mods.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Actors
{
    public static class ActorExtentions
    {
        public static Age[] GetValidAges(this CustomizeStruct data)
        {
            if (data.Tribe == Tribes.Midlander)
                return [Age.Normal, Age.Old, Age.Young];

            if(data.Race == Races.Elezen)
                return [Age.Normal, Age.Old, Age.Young];

            if (data.Race == Races.AuRa)
                return [Age.Normal, Age.Old, Age.Young];

            if (data.Race == Races.Miqote && data.Gender == Genders.Feminine)
                return [Age.Normal, Age.Young];

            return [Age.Normal];
        }

        public static Tribes[] GetValidTribes(this CustomizeStruct data)
        {
            Races x = data.Race;
            var firstValid = (Tribes)((byte)x * 2 - 1);

            return
            [
                firstValid,
                (Tribes)((byte)firstValid + 1)
            ];
        }

        public static Genders[] GetValidGenders(this CustomizeStruct data)
        {
            Races x = data.Race;


            //female hrothgar is here!
            //if (x == Races.Hrothgar)
            //    return [Genders.Masculine];

            return [Genders.Masculine, Genders.Feminine];
        }

        public static RaceCodes GetRaceCode(this CustomizeStruct data)
        {
            RaceCodes code = RaceCodes.C0101;

            switch(data.Race)
            {
                case Races.Hyur:
                    if(data.Tribe == Tribes.Highlander)
                    {
                        code = data.Gender == Genders.Masculine ? RaceCodes.C0301 : RaceCodes.C0401;
                    }
                    else
                    {
                        code = data.Gender == Genders.Masculine ? RaceCodes.C0101 : RaceCodes.C0201;
                    }
                    break;
                case Races.Elezen:
                    code = data.Gender == Genders.Masculine ? RaceCodes.C0501 : RaceCodes.C0601;
                    break;
                case Races.Miqote:
                    code = data.Gender == Genders.Masculine ? RaceCodes.C0701 : RaceCodes.C0801;
                    break;
                case Races.Roegadyn:
                    code = data.Gender == Genders.Masculine ? RaceCodes.C0901 : RaceCodes.C1001;
                    break;
                case Races.Lalafel:
                    code = data.Gender == Genders.Masculine ? RaceCodes.C1101 : RaceCodes.C1201;
                    break;
                case Races.AuRa:
                    code = data.Gender == Genders.Masculine ? RaceCodes.C1301 : RaceCodes.C1401;
                    break;
                case Races.Hrothgar:
                    code = data.Gender == Genders.Masculine ? RaceCodes.C1501 : RaceCodes.C1601;
                    break;
                case Races.Viera:
                    code = data.Gender == Genders.Masculine ? RaceCodes.C1701 : RaceCodes.C1801;
                    break;
            }

            return code;
        }
    }
}
