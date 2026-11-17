namespace Citron;
internal sealed class Preferences
{
    public string Language { get; set; } = Strings.Language;
    public int Interval { get; set; } = 60;
    public int Duration { get; set; }
    public bool KeepAwake { get; set; }
    public bool KeepDisplayOn { get; set; }
    public bool SetupPrompted { get; set; }
    public bool RandomizeInterval { get; set; }
}
