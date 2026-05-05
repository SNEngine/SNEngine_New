using Avalonia.Markup.Xaml;
using SNEngine.Studio.Views.Dialogs;

namespace SNEngine.Studio.Views;

public partial class BuildDialog : DialogWindowBase
{
    public BuildDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}