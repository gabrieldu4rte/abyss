using Godot;
using System;

public partial class Main
{
    int helpTopic;
    void HandleHelp(Key key)
    {
        if(Previous(key))helpTopic=(helpTopic+6)%7;
        if(Next(key))helpTopic=(helpTopic+1)%7;
    }
    string HelpText(int topic)=>topic switch
    {
        0=>T("# O ABISMO\nSob as ruinas, escadas levam a saloes esquecidos.\nA luz da superficie desaparece muito antes do fim da jornada.\n\n# A DESCIDA\nProcure as passagens entre as salas e guarde suas provisoes.\nNem toda sombra precisa ser enfrentada.\nAs escadas sao o caminho para as profundezas.\n\n# O QUE RESTA\nOuro, cicatrizes e lembrancas acompanham quem sobrevive.\nA verdadeira conquista e voltar a encontrar o proximo caminho.",
            "# THE ABYSS\nBeneath the ruins, stairs lead to forgotten halls.\nDaylight vanishes long before the journey is over.\n\n# THE DESCENT\nSeek passages between chambers and preserve your supplies.\nNot every shadow must be confronted.\nStairs lead farther into the depths.\n\n# WHAT REMAINS\nGold, scars and memories follow those who survive.\nThe true achievement is finding the next path."),
        1=>T("# SALAS E CORREDORES\nSalas abertas oferecem espaco, mas tambem deixam voce exposto.\nPassagens estreitas ajudam a separar os perseguidores.\nParedes e esquinas podem esconder sua fuga.\n\n# ESCADAS VIGIADAS\nAlgumas escadas pertencem aos Guardioes.\nEles aguardam na propria sala e nao abandonam seu posto.\nEntrar em seu dominio rompe o silencio.\n\n# UM REFUGIO RARO\nA luz do mercador anuncia uma tregua.\nAli nao ha criaturas a espreita: apenas negocios e outra escada.",
            "# CHAMBERS AND CORRIDORS\nOpen rooms offer space, but leave you exposed.\nNarrow passages help separate pursuers.\nWalls and corners may conceal your retreat.\n\n# GUARDED STAIRS\nSome stairways belong to the Wardens.\nThey wait in their chamber and never abandon their post.\nEntering their domain breaks the silence.\n\n# A RARE REFUGE\nThe merchant's light promises a truce.\nNo creatures lurk there: only trade and another stairway."),
        2=>T("# RATOS DAS CRIPTAS\nPequenos vultos percorrem o chao entre pedras e ossos.\nNao confunda seu tamanho com mansidao.\n\n# ESQUELETOS\nOs mortos ainda caminham pelos corredores.\nSeus passos secos denunciam presencas alem da luz.\n\n# GOBLINS\nOlhos atentos acompanham qualquer movimento nas salas.\nQuem e visto pode ganhar um perseguidor persistente.\n\n# GUARDIOES\nSentinelas das escadas, presos ao dever de proteger a passagem.\nPara seguir adiante, sera preciso enfrenta-los.",
            "# CRYPT RATS\nSmall shapes scurry among stones and bones.\nDo not mistake their size for gentleness.\n\n# SKELETONS\nThe dead still walk these corridors.\nDry footsteps betray a presence beyond the light.\n\n# GOBLINS\nWatchful eyes follow every movement in the chambers.\nBeing spotted may earn you a persistent pursuer.\n\n# WARDENS\nStairway sentinels, bound to guard the passage.\nTo travel farther, you must confront them."),
        3=>T("# GUERREIRO\nEnfrenta o perigo de perto, com espada ou adaga.\nArmaduras de placas combinam com sua vocacao.\n\n# MAGO\nConduz a forca arcana por meio de um cajado.\nMantos protegem quem trilha esse caminho.\n\n# ARQUEIRO\nMantem a distancia e confia na precisao do arco.\n\n# LADINO\nEspadas e adagas acompanham seus passos discretos.\nA astucia pode valer tanto quanto a forca.",
            "# WARRIOR\nFaces danger up close, with sword or dagger.\nPlate armor suits this calling.\n\n# MAGE\nChannels arcane force through a staff.\nRobes shelter those who walk this path.\n\n# ARCHER\nKeeps a distance and trusts the bow's precision.\n\n# ROGUE\nSwords and daggers accompany quiet footsteps.\nCunning can be as valuable as strength."),
        4=>T("# ARMAS, VESTES E AMULETOS\nUma boa lamina, um manto ou um talisma podem mudar sua jornada.\nAlgumas pecas exigem experiencia; outras, uma vocacao especifica.\n\n# TESOUROS\nBaus esquecidos guardam equipamentos e provisoes.\nNem tudo o que encontrar servira ao seu caminho.\nO mercador pode dar destino ao que voce nao precisa.\n\n# RELIQUIAS\nPecas comuns e raras dividem espaco com achados extraordinarios.\nArtefatos epicos e lendarios carregam propriedades especiais.\nExamine cada um: poder e utilidade nem sempre sao a mesma coisa.",
            "# WEAPONS, GARMENTS AND CHARMS\nA good blade, robe or talisman can change your journey.\nSome pieces demand experience; others, a particular calling.\n\n# TREASURES\nForgotten chests hold equipment and supplies.\nNot everything you find will suit your path.\nThe merchant may find a use for what you do not need.\n\n# RELICS\nCommon and rare pieces mingle with extraordinary discoveries.\nEpic and legendary artifacts carry special properties.\nExamine each: power and usefulness are not always the same."),
        5=>T("# FRASCOS DE VIDA\nPocoes fecham feridas e devolvem folego aos viajantes.\nGuarde-as com cuidado; a proxima pode estar longe.\n\n# ESSENCIAS DE ENERGIA\nOutros frascos restauram as forcas gastas na jornada.\nCristais encontrados pelo caminho tambem oferecem alivio.\n\n# MOEDAS\nO ouro pesa pouco diante de uma necessidade urgente.\nUm bolso abastecido pode garantir provisoes no proximo refugio.\n\n# PRUDENCIA\nEscolha quando lutar e quando recuar.\nNenhum tesouro vale muito para quem nao sobrevive para usa-lo.",
            "# HEALTH DRAUGHTS\nPotions close wounds and restore a traveler's breath.\nGuard them carefully; the next may be far away.\n\n# ENERGY ESSENCES\nOther bottles replenish strength spent on the journey.\nCrystals found along the way also offer relief.\n\n# COINS\nGold weighs little against an urgent need.\nA full purse may secure supplies at the next refuge.\n\n# PRUDENCE\nChoose when to fight and when to retreat.\nTreasure means little to those who cannot survive to use it."),
        _=>T("# O MERCADOR\nUm viajante de capuz mantem seu pequeno refugio entre as ruinas.\nNinguem sabe como suas mercadorias chegam tao fundo.\n\n# SUA BANCA\nHa armas, vestes, amuletos e frascos entre seus pertences.\nSua oferta varia: examine as pecas antes de se decidir.\nEle tambem compra equipamentos e provisoes dos aventureiros.\n\n# PALAVRAS NA PENUMBRA\n\"O Abismo cobra caro. Eu aceito moedas.\"\n\"Volte vivo. Bons clientes sao raros.\"\n\n# TREGUA\nA sala do mercador e um lugar seguro antes de continuar a descida.",
            "# THE MERCHANT\nA hooded traveler keeps a small refuge among the ruins.\nNobody knows how his wares reach these depths.\n\n# HIS STALL\nWeapons, garments, charms and bottles fill his belongings.\nHis wares vary: examine each piece before deciding.\nHe also buys equipment and supplies from adventurers.\n\n# WORDS IN THE GLOOM\n\"The Abyss asks a price. I take coins.\"\n\"Come back alive. Good customers are rare.\"\n\n# TRUCE\nThe merchant's room is a safe place before continuing downward.")
    };
    void DrawHelpTopics()
    {
        string[] topics={T("O ABISMO","THE ABYSS"),T("CAMINHOS E REFUGIOS","PATHS AND REFUGES"),T("HABITANTES","INHABITANTS"),T("VOCACOES","CALLINGS"),T("EQUIPAMENTOS E RELIQUIAS","EQUIPMENT AND RELICS"),T("PROVISOES","SUPPLIES"),T("O MERCADOR","THE MERCHANT")};
        Text(32,206,T("COMPENDIO DO VIAJANTE","TRAVELER'S COMPENDIUM"),gold,17);
        for(int i=0;i<topics.Length;i++)Text(32,251+i*38,(helpTopic==i?"> ":"  ")+topics[i],helpTopic==i?teal:dim,15);
        Text(32,572,T("[CIMA/BAIXO] categoria","[UP/DOWN] category"),dim,14);
        float y=212;
        foreach(string line in HelpText(helpTopic).Split('\n'))
        {
            Text(355,y,line.StartsWith("# ")?line[2..]:line,line.StartsWith("# ")?gold:ink,16);y+=29;
        }
    }
}
