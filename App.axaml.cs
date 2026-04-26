using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RospSqlGenerator.Services;
using RospSqlGenerator.ViewModels;
using RospSqlGenerator.Views;

namespace RospSqlGenerator;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var workbookReader = new ClosedXmlRospWorkbookReader();
            var sqlGenerator = new PostgreSqlRospSqlGenerator();
            var fileDialogService = new AvaloniaFileDialogService(desktop);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(
                    workbookReader,
                    sqlGenerator,
                    fileDialogService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}