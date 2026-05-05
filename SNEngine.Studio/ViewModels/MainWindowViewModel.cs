using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using SNEngine.API;
using SNEngine.Assets.Package;
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
    public ReactiveCommand<Unit, Unit> BuildProjectCommand { get; }

    public MainViewModel()
    {
        CreateNewProjectCommand = ReactiveCommand.CreateFromTask(CreateNewProjectAsync);
        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProjectAsync);
        BuildProjectCommand = ReactiveCommand.CreateFromTask(BuildProjectAsync);
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

    private async Task BuildProjectAsync()
    {
        if (CurrentProject == null || string.IsNullOrEmpty(CurrentProject.ProjectPath))
        {
            StatusText = "❌ Нет открытого проекта для сборки";
            return;
        }

        StatusText = "🔨 Сборка проекта...";

        try
        {
            string assetsPath = Path.Combine(CurrentProject.ProjectPath, "assets");

            if (!Directory.Exists(assetsPath))
            {
                StatusText = "❌ Папка assets не найдена в проекте";
                return;
            }

            // Прямой вызов сборщика
            PakBuilder.PackSmart(assetsPath, Path.Combine(CurrentProject.ProjectPath, "build"));

            StatusText = $"✅ Сборка завершена! Пакеты .snpk созданы в папке build";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Ошибка сборки: {ex.Message}";
        }
    }
}