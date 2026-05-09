namespace SNEngine.Backend.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? OutputPath { get; set; }
    public double ElapsedSeconds { get; set; }
}