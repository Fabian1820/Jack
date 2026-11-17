using Microsoft.UI.Xaml;

namespace Jack.WinUI;

public partial class App : Application
{
    private Window? window;

    public App()
    {
        // Registro mínimo de fallos de arranque para depurar sin ver la ventana.
        UnhandledException += (_, e) => File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.log"), e.Exception.ToString());
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        window = new MainWindow();
        window.Activate();
    }
}
