using Microsoft.AspNetCore.Mvc;
using SNEngine.Backend.Models;
using SNEngine.Backend.Services;
using Microsoft.Extensions.Logging;

namespace SNEngine.Backend.Controllers;

[ApiController]
[Route("api/build")]
public class BuildController : ControllerBase
{
    private readonly GameBuildService _buildService;
    private readonly ILogger<BuildController> _logger;

    public BuildController(GameBuildService buildService, ILogger<BuildController> logger)
    {
        _buildService = buildService;
        _logger = logger;
        _logger.LogInformation("BuildController initialized");
    }

    [HttpPost]
    public async Task<IActionResult> Build([FromBody] BuildRequest request)
    {
        _logger.LogInformation("=== Build request received ===");
        _logger.LogInformation("ProjectPath: {path}", request?.ProjectPath);
        _logger.LogInformation("Platform: {platform}", request?.Platform ?? "windows");

        if (request == null || string.IsNullOrWhiteSpace(request.ProjectPath))
        {
            _logger.LogWarning("Invalid request: ProjectPath is required");
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "ProjectPath is required"
            });
        }

        var response = await _buildService.BuildAsync(request);

        _logger.LogInformation("Build finished. Success: {success}", response.Success);

        return response.Success ? Ok(response) : BadRequest(response);
    }
}