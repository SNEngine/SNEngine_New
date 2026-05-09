using SNEngine.Scripting.AssemblyBuilder.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SNEngine.Scripting.AssemblyBuilder.Pipeline;

/// <summary>
/// Default build pipeline with automatic stage discovery via reflection + [BuildStage] attribute.
/// </summary>
public class DefaultBuildPipeline : IBuildPipeline
{
    private readonly IReadOnlyList<ISnBuildStage> _stages;

    public DefaultBuildPipeline()
    {
        _stages = DiscoverStages();
    }

    private static IReadOnlyList<ISnBuildStage> DiscoverStages()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(ISnBuildStage).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract)
            .Select(t => new
            {
                Type = t,
                Attr = t.GetCustomAttribute<BuildStageAttribute>()
            })
            .Where(x => x.Attr != null)
            .OrderBy(x => x.Attr!.Order)
            .Select(x => (ISnBuildStage)Activator.CreateInstance(x.Type)!)
            .ToList();
    }
    public async Task<BuildResult> ExecuteAsync(BuildContext context)
    {
        foreach (var stage in _stages)
        {
            try
            {
                context.Log.OnNext($"[Stage] {stage.Name} started");
                await stage.ExecuteAsync(context);
                context.Log.OnNext($"[Stage] {stage.Name} completed");
            }
            catch (Exception ex)
            {
                context.Log.OnNext($"[Stage] {stage.Name} FAILED: {ex.Message}");
                return new BuildResult(false, 0);
            }
        }

        return new BuildResult(true, 0);
    }
}