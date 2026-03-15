namespace SkillShaper.AutoDj.Models;

public sealed class TrackInfo
{
    public string Path { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public double EstimatedBpm { get; init; } = 120.0;
}
