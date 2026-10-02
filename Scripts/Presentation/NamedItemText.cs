namespace Abyss.Presentation;
internal static class NamedItemText
{
    internal static string Name(ItemId id, bool en) => (en ? new[] { "", "Spark Sword", "Venom Bow", "Insulating Leather", "Camp Ring", "Staff of Fluid Control", "Precise Throwing Gauntlets", "Deep Breath Mantle", "Executioner's Blade", "Spellforge Blade", "Revenge Hunter's Bow", "Sandflower Relic", "Hood of Shadow Legends", "Explorer Blade", "Vigor Bracers", "Continuous Flame Buckle", "Investigator Mantle", "Ember Staff", "Twilight Bow", "Hunter's Greed Amulet", "Thick Rubber Boots", "Conductive Crossbow", "Fungal Sovereign Crown", "Eternal Forge Robe" } : new[] { "", "Espada da Centelha", "Arco Peconhento", "Couro Isolante", "Anel do Acampamento", "Cajado do Controle de Fluidos", "Manoplas do Arremesso Preciso", "Manto do Folego Profundo", "Lamina do Algoz", "Lamina da Forja das Magias", "Arco do Cacador da Revanche", "Reliquia da Flor das Areias", "Capuz das Lendas Sombrias", "Lamina do Explorador", "Bracadeiras do Vigor", "Fivela do Fogo Continuo", "Manto do Investigador", "Cajado das Brasas", "Arco do Crepusculo", "Amuleto da Cobica do Cacador", "Botas de Borracha Grossa", "Balestra Condutora", "Coroa do Soberano Fungico", "Robe da Forja Eterna" })[(int)id];
    internal static string Effect(ItemId id, bool en) => id switch {
        ItemId.SparkSword => en ? "Physical hits on oil: 50% chance to ignite it without an extra action." : "Acertos fisicos sobre oleo: 50% de chance de incendiar sem acao extra.",
        ItemId.VenomBow => en ? "Natural 20: poison for 2 damage over 3 turns. Fungal creatures are immune." : "20 natural: veneno de 2 dano por 3 turnos. Criaturas fungicas sao imunes.",
        ItemId.InsulatingLeather => en ? "Immune to discharge splash; fire damage over time is reduced by 1. Direct discharge still hurts." : "Imune a descarga espalhada; -1 dano continuo de fogo. A descarga direta ainda fere.",
        ItemId.CampRing => en ? "Merchant torches cost 5 gold. Each torch lasts 120 turns while equipped." : "Tochas do mercador custam 5 ouro. Cada tocha dura 120 turnos enquanto equipado.",
        ItemId.FluidStaff => en ? "Basic shots extinguish fire along their path and leave water for 2 turns." : "Disparos basicos apagam fogo no caminho e deixam agua por 2 turnos.",
        ItemId.ThrowingGauntlets => en ? "Thrown torches pierce visible enemies until a wall, dealing 1 fire damage and pushing them one cell." : "Tochas atravessam inimigos visiveis ate a parede, causando 1 dano de fogo e empurrando uma casa.",
        ItemId.DeepBreathMantle => en ? "Immune to Thin Air. Energy potions roll 3d6 and keep the highest two." : "Imune ao Ar Rarefeito. Pocoes de energia rolam 3d6 e somam os dois maiores.",
        ItemId.ExecutionerBlade => en ? "Rogue attacks cost 1 HP before the roll and gain +1d6 on hit. The cost can be lethal." : "Ataques do Ladino custam 1 PV antes da rolagem e ganham +1d6 ao acertar. O custo pode ser letal.",
        ItemId.SpellforgeBlade => en ? "Critical hits release a seismic impact: 2d4 + STR in radius 2, pushing enemies one cell. No energy cost." : "Criticos liberam impacto sismico: 2d4 + FOR em raio 2 e empurrao de uma casa. Sem custo de energia.",
        ItemId.RevengeBow => en ? "Advantage against elites and Wardens. Basic shots keep full accuracy and damage at any range." : "Vantagem contra elites e Guardioes. Disparos basicos mantem acerto e dano em qualquer distancia.",
        ItemId.SandflowerRelic => en ? "Actual healing blinds nearby enemies for 5 turns and reveals nearby walls. Floor recovery does not trigger it." : "Cura efetiva cega inimigos proximos por 5 turnos e revela paredes. Recuperacao ao descer nao ativa.",
        ItemId.ShadowLegendsHood => en ? "Lethal damage without a lit torch: escape to the stair room at 1 HP, lose all energy and destroy the hood." : "Dano letal sem tocha acesa: fuga para a sala da escada com 1 PV, perde toda energia e destroi o capuz.",
        ItemId.ExplorerBlade => en ? "Melee attacks against targets on water have advantage and +2 damage." : "Ataques corpo a corpo contra alvos na agua tem vantagem e +2 dano.",
        ItemId.VigorBracers => en ? "Health potions heal 2d10 +4, capped at maximum HP." : "Pocoes de vida curam 2d10 +4, limitado aos PV maximos.",
        ItemId.ContinuousFlameBuckle => en ? "Automatically replacing a burned-out torch restores 1d6 energy." : "Trocar automaticamente uma tocha esgotada por uma reserva restaura 1d6 energia.",
        ItemId.InvestigatorMantle => en ? "You neither trigger nor take damage from ruin spikes; they remain armed." : "Voce nao ativa nem sofre dano de espinhos das ruinas; eles continuam armados.",
        ItemId.EmberStaff => en ? "Kills on burning ground restore 2 energy while this staff is equipped." : "Abates em chao em chamas restauram 2 energia enquanto equipado.",
        ItemId.TwilightBow => en ? "Ranged hits in visible dim light deal +3 damage: beyond 5 tiles with a torch, 3 without." : "Acertos a distancia na penumbra visivel: +3 dano alem de 5 casas com tocha, 3 sem.",
        ItemId.HunterGreedAmulet => en ? "Elite enemies always drop a rare-or-better item." : "Elites sempre deixam um equipamento raro ou melhor.",
        ItemId.ThickRubberBoots => en ? "Immune to all cistern discharge damage, including direct hits." : "Imune a todo dano de descargas das cisternas, inclusive impactos diretos.",
        ItemId.ConductiveCrossbow => en ? "Ranged hits on water discharge electricity into the target and adjacent cells." : "Acertos a distancia na agua descarregam eletricidade no alvo e nas casas adjacentes.",
        ItemId.FungalSovereignCrown => en ? "Poison immunity. Consume spore traps to heal 2d10 instead." : "Imune a veneno. Consome armadilhas de esporos para curar 2d10.",
        ItemId.EternalForgeRobe => en ? "Immune to fire. Oil ignited by your thrown torches burns for 6 turns." : "Imune a fogo. Oleo incendiado por suas tochas arremessadas queima por 6 turnos.",
        _ => ""
    };
}
