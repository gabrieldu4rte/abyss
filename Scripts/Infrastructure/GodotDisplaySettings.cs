using Godot;
namespace Abyss.Infrastructure;
internal sealed class GodotDisplaySettings(string path = "user://display.cfg") : IDisplaySettings
{
    public DisplayPreferences? Load()
    {
        var file = new ConfigFile();
        if (file.Load(path) != Error.Ok) return null;
        return new DisplayPreferences(file.GetValue("display", "fullscreen", false).AsBool(),
            new Vector2I(file.GetValue("display", "width", 1280).AsInt32(), file.GetValue("display", "height", 800).AsInt32()));
    }
    public bool Save(DisplayPreferences preferences)
    {
        var file = new ConfigFile();
        file.SetValue("display", "fullscreen", preferences.Fullscreen);
        file.SetValue("display", "width", preferences.Resolution.X);
        file.SetValue("display", "height", preferences.Resolution.Y);
        return file.Save(path) == Error.Ok;
    }
}
