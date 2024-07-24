using Dalamud.Game.ClientState.Objects.Enums;
using IVPlugin.ActorData;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System.Linq;

namespace IVPlugin.Resources.Sheets;

[Sheet("CharaMakeType")]
public class CharaMakeTypeData : ExcelRow
{
    public const int MenuCount = 28;
    public const int SubMenuParamCount = 100;
    public const int SubMenuGraphicCount = 10;
    public const int VoiceCount = 12;
    public const int FaceCount = 8;
    public const int FaceFeatureCount = 7;

    public LazyRow<Race> Race { get; private set; } = null!;
    public LazyRow<Tribe> Tribe { get; private set; } = null!;
    public Genders Gender { get; private set; }

    public LazyRow<Lobby>[] Lobbys { get; } = new LazyRow<Lobby>[MenuCount];

    public byte[] InitVals { get; } = new byte[MenuCount];

    public MenuType[] SubMenuType { get; } = new MenuType[MenuCount];

    public byte[] SubMenuNum { get; } = new byte[MenuCount];

    public byte[] LookAt { get; } = new byte[MenuCount];

    public uint[] SubMenuMask { get; } = new uint[MenuCount];

    public CustomizeIndex[] Customize { get; } = new CustomizeIndex[MenuCount];

    public uint[,] SubMenuParam { get; } = new uint[MenuCount, SubMenuParamCount];

    public byte[,] SubMenuGraphic { get; } = new byte[MenuCount, SubMenuGraphicCount];

    public byte[] Voice { get; } = new byte[VoiceCount];

    public int[,] FacialFeature { get; } = new int[FaceCount, SubMenuGraphicCount];


    public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language);

        Race = new LazyRow<Race>(gameData, parser.ReadColumn<int>(0), language);
        Tribe = new LazyRow<Tribe>(gameData, parser.ReadColumn<int>(1), language);
        Gender = (Genders)parser.ReadColumn<sbyte>(2);

        for (int i = 0; i < MenuCount; i++)
            Lobbys[i] = new LazyRow<Lobby>(gameData, parser.ReadColumn<uint>(3 + i), language);

        for (int i = 0; i < MenuCount; i++)
            InitVals[i] = parser.ReadColumn<byte>(31 + i);

        for (int i = 0; i < MenuCount; i++)
            SubMenuType[i] = (MenuType)parser.ReadColumn<byte>(59 + i);

        for (int i = 0; i < MenuCount; i++)
            SubMenuNum[i] = parser.ReadColumn<byte>(87 + i);

        for (int i = 0; i < MenuCount; i++)
            LookAt[i] = parser.ReadColumn<byte>(115 + i);

        for (int i = 0; i < MenuCount; i++)
            SubMenuMask[i] = parser.ReadColumn<uint>(143 + i);

        for (int i = 0; i < MenuCount; i++)
            Customize[i] = (CustomizeIndex)parser.ReadColumn<uint>(171 + i);

        for (int i = 0; i < MenuCount; i++)
            for (int x = 0; x < SubMenuParamCount; x++)
                SubMenuParam[i, x] = parser.ReadColumn<uint>(199 + (x * MenuCount) + i);

        for (int i = 0; i < MenuCount; i++)
            for (int x = 0; x < SubMenuGraphicCount; x++)
                SubMenuGraphic[i, x] = parser.ReadColumn<byte>(2999 + (x * MenuCount) + i);

        for (int i = 0; i < VoiceCount; i++)
            Voice[i] = parser.ReadColumn<byte>(3279 + i);

        for (int i = 0; i < FaceCount; i++)
            for (int x = 0; x < FaceFeatureCount; x++)
                FacialFeature[i, x] = parser.ReadColumn<int>(3291 + (x * FaceCount) + i);
    }

    public MenuCollection BuildMenus()
    {
        var menus = new Menu[MenuCount];

        for (int i = 0; i < MenuCount; ++i)
        {
            var lobby = Lobbys[i].Value;

            if (lobby == null)
                continue;


            var title = lobby.Text?.RawString ?? "Unknown";
            var menuType = SubMenuType[i];
            var subMenuNum = SubMenuNum[i];
            var subMenuMask = SubMenuMask[i];
            var customizeIndex = Customize[i];
            var initialValue = InitVals[i];
            var subParams = new int[subMenuNum];
            var subGraphics = new byte[SubMenuGraphicCount];

            for (int x = 0; x < subMenuNum; ++x)
            {
                if (x >= SubMenuParamCount)
                {
                    subParams[x] = 0;
                    continue;
                }

                subParams[x] = (int)SubMenuParam[i, x];
            }

            menus[i] = new Menu(i, RowId, title, (Races)Race.Row, (Tribes)Tribe.Row, Gender, lobby, menuType, subMenuMask, customizeIndex, initialValue, subParams, subGraphics, Voice, FacialFeature);
        }

        return new MenuCollection(menus);
    }

    public class MenuCollection(Menu[] menus)
    {
        public Menu[] Menus { get; } = menus;

        public Menu? GetMenuForCustomize(CustomizeIndex index)
        {
            return Menus.FirstOrDefault(x => x.CustomizeIndex == index);
        }

        public Menu[] GetMenusForCustomize(CustomizeIndex index)
        {
            return Menus.Where(x => x.CustomizeIndex == index).ToArray();
        }

        public MenuType GetMenuTypeForCustomize(CustomizeIndex index)
        {
            return GetMenuForCustomize(index)?.Type ?? MenuType.Unknown;
        }
    }

    public record class Menu(int MenuId, uint CharaMakeRow, string Title, Races Race, Tribes Tribe, Genders Gender, Lobby Lobby, MenuType Type, uint MenuMask, CustomizeIndex CustomizeIndex, byte InitialValue, int[] SubParams, byte[] SubGraphics, byte[] Voices, int[,] FacialFeatures);

    public enum MenuType : byte
    {
        List = 0,
        ItemSelect = 1,
        Color = 2,
        Unknown3 = 3,
        MultiItemSelect = 4,
        Numerical = 5,
        Unknown,
    }
}
