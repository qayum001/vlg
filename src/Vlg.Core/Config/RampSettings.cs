namespace Vlg.Core.Config;

public sealed record RampSettings
{
    public required int Start { get; init; }
    public required int Step { get; init; }
    public required int Max { get; init; }
}
