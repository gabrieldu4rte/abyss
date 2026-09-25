using Godot;
using System;
using System.Collections.Generic;

public partial class Main
{
    int pauseTab, journalPage;
    const int JournalPageSize=17;

    void HandlePause(Key key)
    {
        if(key==Key.Escape){screen="game";return;}
        if(key==Key.I){pauseTab=1;return;}
        int previous=pauseTab;
        if(key==Key.Left||key==Key.A)pauseTab=(pauseTab+4)%5;
        if(key==Key.Right||key==Key.D||key==Key.Tab)pauseTab=(pauseTab+1)%5;
        if(key>=Key.Key1&&key<=Key.Key5)pauseTab=(int)key-(int)Key.Key1;
        if(previous!=pauseTab){menuIndex=0;return;}
        if(pauseTab==1)HandleInventory(key);
        if(pauseTab==2)
        {
            int pages=Math.Max(1,(JournalLines().Count+JournalPageSize-1)/JournalPageSize);
            if(Previous(key))journalPage=Math.Max(0,journalPage-1);
            if(Next(key))journalPage=Math.Min(pages-1,journalPage+1);
            if(key==Key.Home)journalPage=0;
            if(key==Key.End)journalPage=pages-1;
        }
        if(pauseTab==3)HandleHelp(key);
        if(pauseTab==4)
        {
            if(Previous(key)||Next(key))menuIndex=1-menuIndex;
            if(Confirm(key))
            {
                if(menuIndex==0)OpenLanguage("pause");
                else AskReturnToMenu();
            }
        }
    }

    List<string> JournalLines()
    {
        var lines=new List<string>();
        foreach(var entry in log)
        {
            string remaining="> "+T(entry.Pt,entry.En);
            while(remaining.Length>91)
            {
                int cut=remaining.LastIndexOf(' ',90);
                if(cut<3)cut=91;
                lines.Add(remaining[..cut]);remaining="  "+remaining[cut..].TrimStart();
            }
            lines.Add(remaining);
        }
        return lines;
    }

    void DrawPause()
    {
        Text(32,95,T("PAUSADO","PAUSED"),gold,22);
        string[] tabs={T("[1] PERSONAGEM","[1] CHARACTER"),T("[2] INVENTARIO","[2] INVENTORY"),T("[3] DIARIO","[3] JOURNAL"),T("[4] AJUDA","[4] HELP"),T("[5] CONFIGURACOES","[5] SETTINGS")};
        for(int i=0;i<5;i++)Text(32+i*248,143,(pauseTab==i?"> ":"  ")+tabs[i],pauseTab==i?teal:dim,16);
        Rule(163);
        if(pauseTab==0)DrawCharacterSheet();
        else if(pauseTab==2)DrawJournal();
        else if(pauseTab==3)DrawHelpTopics();
        else if(pauseTab==4)DrawPauseSettings();
        else DrawInventory();
        Footer(
            "[1-5 / A D / TAB] abas   [CIMA/BAIXO] navegar   [ENTER] confirmar   [ESC] continuar",
            "[1-5 / A D / TAB] tabs   [UP/DOWN] navigate   [ENTER] confirm   [ESC] resume");
    }

    void DrawCharacterSheet()
    {
        Portrait(32,202,AsciiArt.Heroes[selected],ClassName(selected),teal);
        Text(32,530,T($"NIVEL {level} / ANDAR {floor}",$"LEVEL {level} / FLOOR {floor}"),gold,18);
        Text(32,563,$"XP {xp} / {XpToNext}",ink,17);
        Text(32,596,T($"DERROTADOS {kills}",$"DEFEATED {kills}"),dim,16);
        Text(310,208,T("ATRIBUTOS / MODIFICADORES","ATTRIBUTES / MODIFIERS"),gold,18);
        Lines(310,245,T(
            $"FORCA         {EffectiveAttributes.Strength,2}  ({Signed(EffectiveAttributes.Str)})\nDESTREZA      {EffectiveAttributes.Dexterity,2}  ({Signed(EffectiveAttributes.Dex)})\nCONSTITUICAO  {EffectiveAttributes.Constitution,2}  ({Signed(EffectiveAttributes.Con)})\nINTELIGENCIA  {EffectiveAttributes.Intelligence,2}  ({Signed(EffectiveAttributes.Int)})",
            $"STRENGTH      {EffectiveAttributes.Strength,2}  ({Signed(EffectiveAttributes.Str)})\nDEXTERITY     {EffectiveAttributes.Dexterity,2}  ({Signed(EffectiveAttributes.Dex)})\nCONSTITUTION  {EffectiveAttributes.Constitution,2}  ({Signed(EffectiveAttributes.Con)})\nINTELLIGENCE  {EffectiveAttributes.Intelligence,2}  ({Signed(EffectiveAttributes.Int)})"),ink,17,29);
        Text(755,208,T("RECURSOS E DEFESAS","RESOURCES AND DEFENSES"),gold,18);
        Lines(755,245,T(
            $"Vida {hp}/{maxHp}   Energia {energy}/{maxEnergy}\nPocoes {potions}   Ouro {coins}\nDefesa {Defense}\nProficiencia {Signed(Proficiency)}",
            $"Health {hp}/{maxHp}   Energy {energy}/{maxEnergy}\nPotions {potions}   Gold {coins}\nDefense {Defense}\nProficiency {Signed(Proficiency)}"),ink,16,29);
        Text(310,393,T("ATAQUES E HABILIDADES","ATTACKS AND ABILITIES"),gold,18);
        Text(310,426,!CanMelee?T("Sem ataque corpo a corpo. Use [F] ou [Q].","No melee attack. Use [F] or [Q]."):T($"Basico adjacente: d20{Signed(MeleeBonus)} | Dano {MeleeDice}",$"Adjacent basic: d20{Signed(MeleeBonus)} | Damage {MeleeDice}"),ink,17);
        Text(310,458,$"[Q] {SkillName(selected)} | {AbilityCost} EN | d20{Signed(SpellBonus+2)} | {AbilityDice}",teal,17);
        Text(310,487,T($"Alcance: {AbilityRange} casas",$"Range: {AbilityRange} tiles")+(selected==3?T(" | Critico 19-20; +4 defesa na resposta"," | Critical 19-20; +4 defense on response"):""),dim,15);
        if(selected is 1 or 2)Text(310,519,CanShoot?T($"[F] Basico: {ShotCost} EN | d20{Signed(SpellBonus)} | {ShotDice} | Alcance {(selected==2?10:6)}",$"[F] Basic: {ShotCost} EN | d20{Signed(SpellBonus)} | {ShotDice} | Range {(selected==2?10:6)}"):T("[F] Equipe um arco/cajado compativel para disparar.","[F] Equip a compatible bow/staff to shoot."),ink,16);
        Text(310,551,T("[P] Pocao: cura 2d10 (2-20 PV), consome uma acao.","[P] Potion: heals 2d10 (2-20 HP), uses one action."),ink,16);
        Text(310,603,T("EQUIPAMENTOS","EQUIPMENT"),gold,16);
        for(int i=0;i<3;i++)Text(310,630+i*22,SlotName(i)+": "+(equipped[i] is Gear g?GearLabel(g):T("Vazio","Empty")),equipped[i] is Gear gear?RarityColor(gear.Quality):dim,15);
    }

    void DrawJournal()
    {
        DrawAsciiImage(32,215,AsciiArt.Book,245,280,new Color("fff6df"),3);
        Text(32,540,T("DIARIO DA EXPEDICAO","EXPEDITION JOURNAL"),gold,16);
        Lines(32,577,T("Mais recentes primeiro.\n[CIMA/BAIXO] paginas\n[HOME/END] inicio/fim","Newest entries first.\n[UP/DOWN] pages\n[HOME/END] first/last"),dim,14,27);
        var lines=JournalLines();
        int pages=Math.Max(1,(lines.Count+JournalPageSize-1)/JournalPageSize);
        journalPage=Math.Clamp(journalPage,0,pages-1);
        Text(310,208,T($"PAGINA {journalPage+1}/{pages} / {log.Count} REGISTROS",$"PAGE {journalPage+1}/{pages} / {log.Count} ENTRIES"),gold,17);
        if(lines.Count==0)Text(310,252,T("Nenhum registro nesta expedicao.","No entries in this expedition."),dim,16);
        for(int i=0;i<JournalPageSize&&journalPage*JournalPageSize+i<lines.Count;i++)
            Text(310,250+i*25,lines[journalPage*JournalPageSize+i],i%2==0?ink:dim,16);
    }

    void DrawPauseSettings()
    {
        DrawAsciiImage(32,200,AsciiArt.Camp,490,470,new Color("fff6df"),2);
        Text(600,233,T("CONFIGURACOES DA EXPEDICAO","EXPEDITION SETTINGS"),gold,24);
        MenuItem(600,300,0,T("IDIOMA","LANGUAGE"));
        MenuItem(600,340,1,T("VOLTAR AO MENU PRINCIPAL","RETURN TO MAIN MENU"));
    }
}
