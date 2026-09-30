using static Abyss.Rules.TabletopRules;
namespace Abyss.Domain;
internal enum CombatAttribute { Strength, Dexterity, Intelligence }
internal sealed record EnemyProfile(string ArtKey, int BaseHealth, int BaseArmor, int DamageSides, int Experience, Attributes Attributes, CombatAttribute AttackAttribute = CombatAttribute.Strength);
