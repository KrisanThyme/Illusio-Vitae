using IVPlugin.ActorData;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace IVPlugin.Resources.Sheets;

[Sheet("HairMakeType")]
public class HairMakeTypeData : ExcelRow
{
    public const int EntryCount = 100;

    public LazyRow<Lumina.Excel.GeneratedSheets.Race> Race { get; private set; } = null!;
    public LazyRow<Lumina.Excel.GeneratedSheets.Tribe> Tribe { get; private set; } = null!;
    public Genders Gender { get; private set; }

    public LazyRow<CharaMakeCustomize>[] HairStyles = new LazyRow<CharaMakeCustomize>[EntryCount];
    public LazyRow<CharaMakeCustomize>[] FacePaints = new LazyRow<CharaMakeCustomize>[EntryCount];


    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language);

        Race = new LazyRow<Lumina.Excel.GeneratedSheets.Race>(gameData, parser.ReadColumn<int>(0), language);
        Tribe = new LazyRow<Lumina.Excel.GeneratedSheets.Tribe>(gameData, parser.ReadColumn<int>(1), language);
        Gender = (Genders)parser.ReadColumn<sbyte>(2);

        for (int i = 0; i < EntryCount; i++)
            HairStyles[i] = new LazyRow<CharaMakeCustomize>(gameData, parser.ReadColumn<uint>(66 + (i * 9)), language);

        for (int i = 0; i < EntryCount; i++)
            FacePaints[i] = new LazyRow<CharaMakeCustomize>(gameData, parser.ReadColumn<uint>(73 + (i * 9)), language);
    }
}
