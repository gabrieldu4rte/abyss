namespace Abyss.Ports;
internal interface IDisplaySettings
{
    DisplayPreferences? Load();
    bool Save(DisplayPreferences preferences);
}
