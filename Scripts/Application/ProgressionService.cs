using System;

namespace Abyss.Application;
internal sealed class ProgressionService
{
    private readonly ExpeditionJournal expeditionJournal;
    private readonly HeroCombatStats heroCombatStats;
    private readonly PlayerState playerState;
    internal ProgressionService(ExpeditionJournal expeditionJournal, HeroCombatStats heroCombatStats, PlayerState playerState)
    {
        this.expeditionJournal = expeditionJournal;
        this.heroCombatStats = heroCombatStats;
        this.playerState = playerState;
    }

    internal void GainXp(int amount)
    {
        playerState.Experience += amount;
        while (playerState.Experience >= heroCombatStats.XpToNext)
        {
            playerState.Experience -= heroCombatStats.XpToNext;
            playerState.Level++;
            if (playerState.Level % 2 == 0)
                playerState.Attributes = playerState.ClassIndex == 0 ? playerState.Attributes with
                {
                    Strength = playerState.Attributes.Strength + 1
                }

                : playerState.ClassIndex == 1 ? playerState.Attributes with
                {
                    Intelligence = playerState.Attributes.Intelligence + 1
                }

                : playerState.Attributes with
                {
                    Dexterity = playerState.Attributes.Dexterity + 1
                };
            else
                playerState.Attributes = playerState.Attributes with
                {
                    Constitution = playerState.Attributes.Constitution + 1
                };
            int growth = 2 + Math.Max(0, playerState.Attributes.Con) / 2;
            playerState.MaxHealth += growth;
            playerState.Health = Math.Min(playerState.MaxHealth, playerState.Health + growth);
            if (playerState.Level % 2 == 0)
                playerState.MaxEnergy++;
            playerState.Energy = Math.Min(playerState.MaxEnergy, playerState.Energy + 1);
            expeditionJournal.Say($"Nivel {playerState.Level}: +1 atributo, +{growth} PV maximos.", $"Level {playerState.Level}: +1 attribute, +{growth} max HP.");
        }
    }
}
