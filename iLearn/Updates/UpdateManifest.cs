namespace iLearn.Updates;

public sealed class UpdateManifest
{
    public string Version { get; set; } = "0.0.0";

    public string Notes { get; set; } = string.Empty;

    public Dictionary<string, string> Downloads { get; set; } = [];
}
