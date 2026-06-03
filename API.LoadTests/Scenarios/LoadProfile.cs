namespace API.LoadTests.Scenarios;

public sealed record LoadProfile
{
    public int WarmupRps { get; init; } = 100;
    public int WarmupSeconds { get; init; } = 30;
    public int Ramp1Rps { get; init; } = 500;
    public int Ramp1Seconds { get; init; } = 60;
    public int Ramp2Rps { get; init; } = 1000;
    public int Ramp2Seconds { get; init; } = 60;
    public int HoldRps { get; init; } = 1000;
    public int HoldSeconds { get; init; } = 60;
    public int QueryRps { get; init; } = 50;

    public int TotalSeconds => WarmupSeconds + Ramp1Seconds + Ramp2Seconds + HoldSeconds;
}
