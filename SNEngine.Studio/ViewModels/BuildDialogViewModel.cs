using ReactiveUI;
using SNEngine.Builder;
using SNEngine.Builder.Strategies;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;

namespace SNEngine.Studio.ViewModels;

public class BuildDialogViewModel : ReactiveObject
{
    private readonly string _projectPath;

    private string _status = "Готов к сборке";
    public string Status
    {
        get => _status;
        private set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    private bool _canBuild = true;
    public bool CanBuild
    {
        get => _canBuild;
        private set => this.RaiseAndSetIfChanged(ref _canBuild, value);
    }

    public string ProjectName { get; }

    public ReactiveCommand<Unit, Unit> BuildCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public BuildDialogViewModel(string projectPath)
    {
        _projectPath = projectPath;
        ProjectName = Path.GetFileName(projectPath);

        BuildCommand = ReactiveCommand.CreateFromTask(BuildAsync);
        CancelCommand = ReactiveCommand.Create(() => { /* закрытие окна */ });
    }

    private async Task BuildAsync()
    {
        CanBuild = false;
        Status = "Сборка...";

        try
        {
            var settings = new BuildSettings
            {
                GameTitle = ProjectName
            };

            var result = await GameBuilder.BuildAsync(_projectPath, "windows", settings);

            Status = result.Success ? "✅ Сборка успешно завершена!" : $"❌ Ошибка: {result.Message}";
        }
        catch (Exception ex)
        {
            Status = $"❌ Критическая ошибка: {ex.Message}";
        }
    }
}