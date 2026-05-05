using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SNEngine.Studio.ViewModels;

namespace SNEngine.Studio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}