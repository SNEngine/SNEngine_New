namespace SNEngine.Builder.Strategies;

public record BuildResult
{
    public bool Success { get; init; }
    public string OutputPath { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
}