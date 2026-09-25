using Godot;

namespace Abyss.Infrastructure;
internal sealed class GodotLanguageSettings : ILanguageSettings
{
    public bool? LoadEnglish(string path)
    {
        var config = new ConfigFile();
        return config.Load(path) == Error.Ok ? config.GetValue("display", "language", "pt").AsString() == "en" : null;
    }

    public bool SaveEnglish(string path, bool english)
    {
        var config = new ConfigFile();
        config.Load(path);
        config.SetValue("display", "language", english ? "en" : "pt");
        return config.Save(path) == Error.Ok;
    }
}
