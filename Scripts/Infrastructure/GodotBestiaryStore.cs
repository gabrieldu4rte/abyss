using Godot;
using System.Collections.Generic;
namespace Abyss.Infrastructure;
internal sealed class GodotBestiaryStore(string path = "user://bestiary.cfg") : IBestiaryStore
{
    public IReadOnlyDictionary<string, int> Load()
    {
        var result = new Dictionary<string, int>();
        var config = new ConfigFile();
        if (config.Load(path) != Error.Ok || !config.HasSection("defeated")) return result;
        foreach (string key in config.GetSectionKeys("defeated"))
            if (int.TryParse(config.GetValue("defeated", key).ToString(), out int count) && count > 0) result[key] = count;
        return result;
    }
    public bool Save(IReadOnlyDictionary<string, int> defeated)
    {
        var config = new ConfigFile();
        foreach (var entry in defeated) config.SetValue("defeated", entry.Key, entry.Value);
        string temporary = path + ".tmp";
        return config.Save(temporary) == Error.Ok
            && DirAccess.RenameAbsolute(ProjectSettings.GlobalizePath(temporary), ProjectSettings.GlobalizePath(path)) == Error.Ok;
    }
}
