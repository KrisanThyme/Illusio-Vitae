using IVPlugin.ActorData;
using Lumina.Excel.GeneratedSheets;

namespace IVPlugin.Resources
{
    public class VoiceData
    {
        public Races race { get; private set; }
        public Genders gender { get; private set; }
        public Tribes tribe { get; private set; }

        public byte[] availableVoices { get; private set; }

        public VoiceData(Races race, Genders gender, Tribes tribe, byte[] availableVoices)
        {
            this.race = race;
            this.gender = gender;
            this.tribe = tribe;
            this.availableVoices = availableVoices;
        }
    }
}
