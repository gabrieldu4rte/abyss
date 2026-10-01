using System.Linq;
namespace Abyss.Presentation;
internal sealed class GameAudioController(IGameAudio? audio, RunState run, MenuState menu, PlayerState player, DungeonState dungeon, VisualEffects effects)
{
    private string screen = "";
    private int level;
    private int descentSteps;
    private double stepDelay;
    private double stepTail;
    internal bool DescentPlaying => descentSteps > 0 || stepTail > 0;
    internal void PlayDescent()
    {
        descentSteps = 3;
        stepDelay = 0;
        stepTail = 0;
    }
    internal void Update(double delta)
    {
        stepTail = System.Math.Max(0, stepTail - delta);
        string next = run.Screen;
        bool expedition = next is "game" or "pause" or "shop" or "torch_aim" or "confirm_exit" || next == "language" && menu.LanguageReturn == "pause";
        string track = !expedition ? "menu" : dungeon.IsMerchantFloor ? "refuge"
            : dungeon.StairsRoom.HasPoint(player.Position) && dungeon.Enemies.Any(e => e.IsWarden && e.Alerted) ? "warden"
            : new[] { "ruins", "cistern", "fungal", "forge" }[(int)dungeon.Environment.Biome];
        audio?.SetVolumes(menu.MusicVolume, menu.EffectsVolume);
        audio?.SetMusic(track);
        if (screen != next && screen.Length > 0) audio?.Play(next == "dead" ? "death" : "confirm");
        if (descentSteps > 0)
        {
            stepDelay -= delta;
            if (stepDelay <= 0)
            {
                audio?.Play("step");
                stepTail = .065;
                descentSteps--;
                stepDelay = .35;
            }
        }
        if (expedition && level > 0 && player.Level > level) audio?.Play("levelup");
        foreach (var cue in effects.Sounds.Drain().Distinct()) audio?.Play(cue);
        screen = next; level = player.Level;
        audio?.Advance(delta);
    }
}
