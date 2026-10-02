namespace Abyss.Presentation;
internal static class AdvancementText
{
    internal static string Name(AdvancedClass choice, bool english) => english ? choice switch
    {
        AdvancedClass.Sentinel => "Sentinel", AdvancedClass.Berserker => "Berserker", AdvancedClass.Pyromancer => "Pyromancer", AdvancedClass.Cryomancer => "Cryomancer",
        AdvancedClass.Ranger => "Ranger", AdvancedClass.Deadeye => "Deadeye", AdvancedClass.Assassin => "Assassin", AdvancedClass.Shadowblade => "Shadowblade", _ => ""
    } : choice switch
    {
        AdvancedClass.Sentinel => "Sentinela", AdvancedClass.Berserker => "Berserker", AdvancedClass.Pyromancer => "Piromante", AdvancedClass.Cryomancer => "Criomante",
        AdvancedClass.Ranger => "Patrulheira", AdvancedClass.Deadeye => "Atiradora", AdvancedClass.Assassin => "Assassino", AdvancedClass.Shadowblade => "Lamina Sombria", _ => ""
    };
    internal static string Primary(AdvancedClass choice, bool en) => choice switch
    {
        AdvancedClass.Sentinel => en ? "Steel whirlwind" : "Redemoinho de aco",
        AdvancedClass.Berserker => en ? "Raging whirlwind" : "Redemoinho furioso",
        AdvancedClass.Pyromancer => en ? "Ember nova" : "Nova ignea",
        AdvancedClass.Cryomancer => en ? "Frost nova" : "Nova glacial",
        AdvancedClass.Ranger => en ? "Hunter's arrow" : "Flecha da cacadora",
        AdvancedClass.Deadeye => en ? "Perfect shot" : "Disparo perfeito",
        AdvancedClass.Assassin => en ? "Death step" : "Passo mortal",
        _ => en ? "Umbral step" : "Passo umbral"
    };
    internal static string Secondary(AdvancedClass choice, bool en) => choice switch
    {
        AdvancedClass.Sentinel => en ? "Bastion" : "Bastiao",
        AdvancedClass.Berserker => en ? "Crushing blow" : "Golpe demolidor",
        AdvancedClass.Pyromancer => en ? "Fireburst" : "Explosao de fogo",
        AdvancedClass.Cryomancer => en ? "Frozen prison" : "Prisao glacial",
        AdvancedClass.Ranger => en ? "Arrow rain" : "Chuva de flechas",
        AdvancedClass.Deadeye => en ? "Death mark" : "Mira fatal",
        AdvancedClass.Assassin => en ? "Venom blade" : "Lamina venenosa",
        _ => en ? "Shadow veil" : "Cortina sombria"
    };
    internal static string Description(AdvancedClass choice, bool en) => choice switch
    {
        AdvancedClass.Sentinel => en ? "Q: stronger whirlwind, radius 3. R: +6 defense for 3 turns." : "Q: redemoinho mais forte, raio 3. R: +6 defesa por 3 turnos.",
        AdvancedClass.Berserker => en ? "Q: a third damage die. R: a heavy adjacent strike that pushes on a hit." : "Q: um terceiro dado de dano. R: golpe pesado adjacente que empurra ao acertar.",
        AdvancedClass.Pyromancer => en ? "Q: a third damage die. R: a ranged burst that burns the target area." : "Q: um terceiro dado de dano. R: explosao a distancia que incendeia a area do alvo.",
        AdvancedClass.Cryomancer => en ? "Q: stronger nova, radius 6. R: damage and freeze nearby foes for 2 turns." : "Q: nova mais forte, raio 6. R: dano e congela inimigos proximos por 2 turnos.",
        AdvancedClass.Ranger => en ? "Q: larger damage dice. R: a volley against all visible foes within 6 tiles." : "Q: dados de dano maiores. R: atinge todos os inimigos visiveis a ate 6 casas.",
        AdvancedClass.Deadeye => en ? "Q: a third die, range 12. R: a powerful shot with +4 accuracy, range 12." : "Q: terceiro dado, alcance 12. R: disparo poderoso com +4 acerto, alcance 12.",
        AdvancedClass.Assassin => en ? "Q: stronger adjacent strike and blink behind. R: adjacent venom strike; fungi are immune to poison." : "Q: golpe adjacente mais forte e teleporte para tras. R: golpe venenoso adjacente; fungos sao imunes ao veneno.",
        _ => en ? "Q: stronger adjacent strike; a successful blink grants +4 defense for 2 turns. R: blind nearby foes for 2 turns." : "Q: golpe adjacente mais forte; teleporte bem-sucedido da +4 defesa por 2 turnos. R: cega inimigos proximos por 2 turnos."
    };
}
