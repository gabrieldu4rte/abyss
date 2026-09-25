namespace Abyss.Ports;
internal interface ILanguageSettings
{
    bool? LoadEnglish(string path);
    bool SaveEnglish(string path, bool english);
}
