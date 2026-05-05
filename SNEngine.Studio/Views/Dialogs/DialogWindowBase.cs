using Avalonia.Controls;

namespace SNEngine.Studio.Views.Dialogs;

public class DialogWindowBase : Window
{
    public DialogWindowBase()
    {
        Width = 500;
        Height = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Title = "SNEngine Studio";
    }
}