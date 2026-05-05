using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using SNEngine.API;
using SNEngine.Builder;
using SNEngine.Builder.Strategies;
using SNEngine.Studio.Models;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;

namespace SNEngine.Studio.ViewModels;

public class MainViewModel : ReactiveObject
{
    private ProjectModel? _currentProject;
    public ProjectModel? CurrentProject
    {
        get => _currentProject;
        private set => this.RaiseAndSetIfChanged(ref _currentProject, value);
    }

    private string _statusText = "Готов к работе";
    public string StatusText
    {
        get => _statusText;
        private set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public ReactiveCommand<Unit, Unit> CreateNewProjectCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenProjectCommand { get; }
    public ReactiveCommand<Unit, Unit> BuildGameCommand { get; }     // ← Сборка игры

    public MainViewModel()
    {
        CreateNewProjectCommand = ReactiveCommand.CreateFromTask(CreateNewProjectAsync);
        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProjectAsync);
        BuildGameCommand = ReactiveCommand.CreateFromTask(BuildGameAsync);
    }

    private async Task CreateNewProjectAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
        if (topLevel?.StorageProvider == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Выберите папку для нового проекта",
            AllowMultiple = false
        });

        if (folders.Count == 0)
        {
            StatusText = "Создание отменено";
            return;
        }

        var selectedFolder = folders[0].Path.LocalPath;

        try
        {
            string projectName = Path.GetFileName(selectedFolder.TrimEnd(Path.DirectorySeparatorChar, '/'));
            if (string.IsNullOrEmpty(projectName)) projectName = "MyNovel";

            ProjectAPI.CreateNewProject(selectedFolder, projectName);

            var projectData = ProjectAPI.LoadProject(Path.Combine(selectedFolder, $"{projectName}.snproj"));

            CurrentProject = new ProjectModel
            {
                ProjectPath = selectedFolder,
                ProjectName = projectName,
                Data = projectData!
            };

            StatusText = $"✅ Проект создан: {projectName}";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Ошибка: {ex.Message}";
        }
    }

    private async Task OpenProjectAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
        if (topLevel?.StorageProvider == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть проект SNEngine",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("SNEngine Project") { Patterns = new[] { "*.snproj" } } }
        });

        if (files.Count == 0)
        {
            StatusText = "Открытие отменено";
            return;
        }

        try
        {
            var projectData = ProjectAPI.LoadProject(files[0].Path.LocalPath);
            if (projectData == null) throw new Exception("Не удалось загрузить проект");

            CurrentProject = new ProjectModel
            {
                ProjectPath = Path.GetDirectoryName(files[0].Path.LocalPath)!,
                ProjectName = projectData.ProjectName,
                Data = projectData
            };

            StatusText = $"✅ Открыт проект: {projectData.ProjectName}";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Ошибка: {ex.Message}";
        }
    }

    private async Task BuildGameAsync()
    {
        if (CurrentProject == null || string.IsNullOrEmpty(CurrentProject.ProjectPath))
        {
            StatusText = "❌ Нет открытого проекта для сборки";
            return;
        }

        StatusText = "🔨 Запуск сборки игры...";

        try
        {
            var settings = new BuildSettings
            {
                GameTitle = CurrentProject.ProjectName,
                Version = "1.0.0"
            };

            // Используем новую систему стратегий
            var result = await GameBuilder.BuildAsync(
                projectPath: CurrentProject.ProjectPath,
                platform: "windows",
                settings: settings);

            if (result.Success)
                StatusText = $"✅ Сборка Windows завершена!\n→ {result.OutputPath}";
            else
                StatusText = $"❌ Ошибка сборки: {result.Message}";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Критическая ошибка сборки: {ex.Message}";
        }
    }
}