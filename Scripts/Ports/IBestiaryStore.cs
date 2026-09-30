using System.Collections.Generic;
namespace Abyss.Ports;
internal interface IBestiaryStore
{
    IReadOnlyDictionary<string, int> Load();
    bool Save(IReadOnlyDictionary<string, int> defeated);
}
