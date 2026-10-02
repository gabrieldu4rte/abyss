using System.Collections.Generic;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Presentation;
internal sealed class Localization
{
    private readonly MenuState menuState;
    internal Localization(MenuState menuState)
    {
        this.menuState = menuState;
    }

    internal bool English => menuState.English;
    internal string Translate(string portuguese, string english) => menuState.English ? english : portuguese;
    internal string HeroName(PlayerState player) => player.AdvancedClass == AdvancedClass.None ? ClassName(player.ClassIndex) : AdvancementText.Name(player.AdvancedClass,English).ToUpperInvariant();
    internal string HeroSkill(PlayerState player) => player.AdvancedClass == AdvancedClass.None ? SkillName(player.ClassIndex) : AdvancementText.Primary(player.AdvancedClass,English);
    internal string ClassName(int i) => menuState.English ? UiTheme.EnglishNames[i] : UiTheme.Names[i];
    internal string SkillName(int i) => menuState.English ? UiTheme.EnglishSkills[i] : UiTheme.Skills[i];
    internal string EnemyName(char glyph) => EnemyText.Name(glyph, 1, English).ToUpperInvariant();
    internal string EnemyName(Enemy? enemy) => enemy == null ? Translate("SEM ALVO", "NO TARGET") : EnemyText.Name(enemy.Glyph, enemy.Depth, English).ToUpperInvariant();
    internal string HelpText(int topic) => topic switch
    {
        7 => Translate("# ADVERSARIOS DE ELITE\nRaros campeoes carregam titulos de cores vivas.\nResistem e golpeiam melhor que os demais.\nAlguns guardam equipamentos raros ou reliquias superiores.\n\n# TERRAS ALTERADAS\nTrevas profundas sufocam ate a luz das tochas.\nUma infestacao reune criaturas de uma unica especie.\nVentos quentes sustentam as chamas por mais tempo.\nO ar rarefeito dificulta recuperar o folego.\nTesouros ocultos recompensam quem explora com cuidado.", "# ELITE ADVERSARIES\nRare champions bear vividly colored titles.\nThey endure and strike harder than their kin.\nSome carry rare equipment or greater relics.\n\n# ALTERED LANDS\nDeep darkness smothers even the light of torches.\nAn infestation gathers creatures of a single kind.\nHot drafts keep flames burning longer.\nThin air makes it harder to regain your breath.\nHidden caches reward careful explorers."),
        0 => Translate("# O ABISMO\nSob as ruinas, escadas levam a saloes esquecidos.\nA luz da superficie desaparece muito antes do fim da jornada.\n\n# A DESCIDA\nProcure as passagens entre as salas e guarde suas provisoes.\nNem toda sombra precisa ser enfrentada.\nAs escadas sao o caminho para as profundezas.\n\n# O QUE RESTA\nOuro, cicatrizes e lembrancas acompanham quem sobrevive.\nA verdadeira conquista e voltar a encontrar o proximo caminho.", "# THE ABYSS\nBeneath the ruins, stairs lead to forgotten halls.\nDaylight vanishes long before the journey is over.\n\n# THE DESCENT\nSeek passages between chambers and preserve your supplies.\nNot every shadow must be confronted.\nStairs lead farther into the depths.\n\n# WHAT REMAINS\nGold, scars and memories follow those who survive.\nThe true achievement is finding the next path."),
        1 => Translate("# SALAS E CORREDORES\nSalas abertas oferecem espaco, mas tambem deixam voce exposto.\nPassagens estreitas ajudam a separar os perseguidores.\nParedes e esquinas podem esconder sua fuga.\n\n# ESCADAS VIGIADAS\nAlgumas escadas pertencem aos Guardioes.\nEles aguardam na propria sala e nao abandonam seu posto.\nEntrar em seu dominio rompe o silencio.", "# CHAMBERS AND CORRIDORS\nOpen rooms offer space, but leave you exposed.\nNarrow passages help separate pursuers.\nWalls and corners may conceal your retreat.\n\n# GUARDED STAIRS\nSome stairways belong to the Wardens.\nThey wait in their chamber and never abandon their post.\nEntering their domain breaks the silence."),
        3 => Translate("# GUERREIRO\nEnfrenta o perigo de perto, com espada ou adaga.\nArmaduras de placas combinam com sua vocacao.\n\n# MAGO\nConduz a forca arcana por meio de um cajado.\nMantos protegem quem trilha esse caminho.\n\n# ARQUEIRO\nMantem a distancia e confia na precisao do arco.\n\n# LADINO\nEspadas e adagas acompanham seus passos discretos.\nAcerta de perto e busca as costas do adversario.", "# WARRIOR\nFaces danger up close, with sword or dagger.\nPlate armor suits this calling.\n\n# MAGE\nChannels arcane force through a staff.\nRobes shelter those who walk this path.\n\n# ARCHER\nKeeps a distance and trusts the bow's precision.\n\n# ROGUE\nSwords and daggers accompany quiet footsteps.\nStrikes up close and seeks the enemy's back."),
        4 => Translate("# ARMAS, VESTES E AMULETOS\nUma boa lamina, um manto ou um talisma podem mudar sua jornada.\nAlgumas pecas exigem experiencia; outras, uma vocacao especifica.\n\n# TESOUROS\nBaus esquecidos guardam equipamentos e provisoes.\nNem tudo o que encontrar servira ao seu caminho.\nO mercador pode dar destino ao que voce nao precisa.\n\n# RELIQUIAS\nPecas comuns e raras dividem espaco com achados extraordinarios.\nArtefatos epicos e lendarios carregam propriedades especiais.\nExamine cada um: poder e utilidade nem sempre sao a mesma coisa.", "# WEAPONS, GARMENTS AND CHARMS\nA good blade, robe or talisman can change your journey.\nSome pieces demand experience; others, a particular calling.\n\n# TREASURES\nForgotten chests hold equipment and supplies.\nNot everything you find will suit your path.\nThe merchant may find a use for what you do not need.\n\n# RELICS\nCommon and rare pieces mingle with extraordinary discoveries.\nEpic and legendary artifacts carry special properties.\nExamine each: power and usefulness are not always the same."),
        5 => Translate("# FRASCOS DE VIDA\nPocoes fecham feridas e devolvem folego aos viajantes.\nGuarde-as com cuidado; a proxima pode estar longe.\n\n# ESSENCIAS DE ENERGIA\nOutros frascos restauram as forcas gastas na jornada.\nCristais encontrados pelo caminho tambem oferecem alivio.\n\n# MOEDAS\nO ouro pesa pouco diante de uma necessidade urgente.\nUm bolso abastecido pode garantir provisoes no proximo refugio.", "# HEALTH DRAUGHTS\nPotions close wounds and restore a traveler's breath.\nGuard them carefully; the next may be far away.\n\n# ENERGY ESSENCES\nOther bottles replenish strength spent on the journey.\nCrystals found along the way also offer relief.\n\n# COINS\nGold weighs little against an urgent need.\nA full purse may secure supplies at the next refuge."),
        _ => Translate("# O MERCADOR\nUm viajante de capuz negocia nas profundezas.\nSua banca oferece armas, vestes, amuletos e provisoes.\nEle tambem compra o que voce nao precisa.\n\n# O FERREIRO PERDIDO\nRunas protegem sua oficina contra criaturas e chamas.\nPor moedas, ele melhora armas, vestes e amuletos.\nQuanto maior a raridade, maior o preco da reforja.\nReliquias unicas resistem ao seu martelo.", "# THE MERCHANT\nA hooded traveler trades in the depths.\nHis stall offers weapons, garments, charms and supplies.\nHe also buys what you no longer need.\n\n# THE LOST BLACKSMITH\nRunes shield his workshop from creatures and flames.\nFor coins, he improves weapons, garments and charms.\nHigher rarities demand a greater reforging price.\nUnique relics resist his hammer.")};    internal static string MonsterName(char glyph, bool english) => EnemyText.Name(glyph, 1, english);
    internal string StatsLine(Attributes a, bool first) => first ? Translate($"FOR {a.Strength}  DES {a.Dexterity}", $"STR {a.Strength}  DEX {a.Dexterity}") : Translate($"CON {a.Constitution}  INT {a.Intelligence}", $"CON {a.Constitution}  INT {a.Intelligence}");
    internal string OfferName(Offer offer) => offer.Gear is Gear g ? GearLabel(g) : offer.Potion == 0 ? Translate("Pocao de vida", "Health potion") : offer.Potion == 1 ? Translate("Pocao de energia", "Energy potion") : Translate("Tocha", "Torch");
    internal string MerchantSpeech() => Translate(new[] { "O Abismo cobra caro. Eu aceito moedas.", "Aco firme, frascos cheios. Escolha bem.", "Aqui, ate as sombras respeitam a tregua.", "Volte vivo. Bons clientes sao raros." }[menuState.MerchantQuote], new[] { "The Abyss asks a price. I take coins.", "Steady steel, full bottles. Choose well.", "Here, even shadows honor the truce.", "Come back alive. Good customers are rare." }[menuState.MerchantQuote]);
    internal string GearStats(Gear gear)
    {
        if (gear.Slot == GearSlot.Weapon)
            return Translate($"Dano: {new DamageDice(1, gear.Sides, gear.Power)} + ", $"Damage: {new DamageDice(1, gear.Sides, gear.Power)} + ") + (gear.Kind == GearKind.Sword ? Translate("FOR", "STR") : gear.Kind == GearKind.Staff ? "INT" : Translate("DES", "DEX"));
        var b = gear.AttributeBonus;
        var parts = new List<string>();
        if (gear.Slot == GearSlot.Armor)
            parts.Add($"{Translate("CA", "AC")} {gear.ArmorClass}");
        if (b.Strength != 0)
            parts.Add(Translate($"FOR +{b.Strength}", $"STR +{b.Strength}"));
        if (b.Dexterity != 0)
            parts.Add(Translate($"DES +{b.Dexterity}", $"DEX +{b.Dexterity}"));
        if (b.Constitution != 0)
            parts.Add($"CON +{b.Constitution}");
        if (b.Intelligence != 0)
            parts.Add($"INT +{b.Intelligence}");
        return string.Join(" | ", parts);
    }

    internal string GearName(Gear g) => g.Special != ItemId.None ? NamedItemText.Name(g.Special, English) : Translate(new[] { "Espada", "Adaga", "Cajado", "Arco", "Placas", "Couro", "Manto", "Amuleto" }[(int)g.Kind], new[] { "Sword", "Dagger", "Staff", "Bow", "Plate", "Leather", "Robe", "Amulet" }[(int)g.Kind]);
    internal string RarityName(Rarity r) => Translate(new[] { "Comum", "Raro", "Epico", "Lendario" }[(int)r], new[] { "Common", "Rare", "Epic", "Legendary" }[(int)r]);
    internal string GearLabel(Gear g) => (g.Special != ItemId.None ? GearName(g) : $"{GearName(g)} / {RarityName(g.Quality)}") + (g.Grade > 0 ? $" +{g.Grade}" : "");
    internal string SlotName(int slot) => Translate(new[] { "ARMA", "ARMADURA", "ACESSORIO" }[slot], new[] { "WEAPON", "ARMOR", "ACCESSORY" }[slot]);
    internal string GearNameFor(Gear gear, bool en)
    {
        bool previous = menuState.English;
        menuState.English = en;
        string name = GearLabel(gear);
        menuState.English = previous;
        return name;
    }

    internal string GearEffect(Gear g) => g.Special != ItemId.None ? NamedItemText.Effect(g.Special, English) : g.Quality < Rarity.Epic ? Translate("Sem efeito especial.", "No special effect.") : g.Slot switch
    {
        GearSlot.Weapon => g.Quality == Rarity.Epic ? Translate("Impacto: +1d4 de dano ao acertar.", "Impact: +1d4 damage on hit.") : Translate("Impacto: +1d6; drena ate 2 PV ao acertar.", "Impact: +1d6; drains up to 2 HP on hit."),
        GearSlot.Armor => Translate($"Protecao: reduz dano recebido em {(g.Quality == Rarity.Epic ? 1 : 2)}.", $"Protection: reduces incoming damage by {(g.Quality == Rarity.Epic ? 1 : 2)}."),
        _ => Translate($"Foco: +{(g.Quality == Rarity.Epic ? 1 : 2)} energia por abate.", $"Focus: +{(g.Quality == Rarity.Epic ? 1 : 2)} energy per kill.")};
}
