using SNEngine.Backend.Models;
using SNEngine.Builder;
using SNEngine.Builder.Strategies;

namespace SNEngine.Backend.Services;

public class GameBuildService
{
    public async Task<ApiResponse> BuildAsync(BuildRequest request)
    {
        var settings = new BuildSettings
        {
            GameTitle = request.GameTitle,
            Version = request.Version
        };

        try
        {
            var result = await GameBuilder.BuildAsync(
                request.ProjectPath,
                request.Platform.ToLowerInvariant(),
                settings);

            return new ApiResponse
            {
                Success = result.Success,
                Message = "Build completed successfully",
                OutputPath = result.OutputPath
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                Success = false,
                Message = $"Build error: {ex.Message}"
            };
        }
    }
}