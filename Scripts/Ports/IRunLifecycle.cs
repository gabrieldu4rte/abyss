namespace Abyss.Ports;
internal interface IRunLifecycle
{
    void Start(int? fixedSeed = null);
    void Descend();
}
