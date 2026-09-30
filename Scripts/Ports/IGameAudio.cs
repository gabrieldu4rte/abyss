namespace Abyss.Ports;
internal interface IGameAudio
{
    void Play(string cue);
    void SetMusic(string track);
    void SetVolumes(int music, int effects);
    void Advance(double delta);
}
