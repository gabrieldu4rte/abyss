namespace Abyss.Rules;
internal sealed record AdvancementProfile(int BaseClass, string ArtKey, int DiceCount, int DiceSides, int Range, int NewDiceCount, int NewDiceSides, int NewRange, int NewCost);
internal static class AdvancementCatalog
{
    internal static AdvancedClass[] Options(int baseClass) => [(AdvancedClass)(1 + baseClass * 2), (AdvancedClass)(2 + baseClass * 2)];
    internal static AdvancementProfile Get(AdvancedClass choice) => choice switch
    {
        AdvancedClass.Sentinel => new(0,"sentinel",2,8,3,0,6,0,6),
        AdvancedClass.Berserker => new(0,"berserker",3,6,2,4,8,1,7),
        AdvancedClass.Pyromancer => new(1,"pyromancer",3,6,5,3,6,6,9),
        AdvancedClass.Cryomancer => new(1,"cryomancer",2,8,6,1,6,3,9),
        AdvancedClass.Ranger => new(2,"ranger",2,10,10,2,6,6,8),
        AdvancedClass.Deadeye => new(2,"deadeye",3,8,12,4,8,12,9),
        AdvancedClass.Assassin => new(3,"assassin",3,8,1,3,6,1,7),
        AdvancedClass.Shadowblade => new(3,"shadowblade",3,6,1,0,6,3,8),
        _ => throw new System.ArgumentOutOfRangeException(nameof(choice))
    };
}
