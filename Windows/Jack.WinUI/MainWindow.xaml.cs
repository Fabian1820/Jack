using Jack.Core;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;

namespace Jack.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly ChatWorkspace workspace = new();

    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        ConversationList.ItemsSource = workspace.Conversations;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1100, 740));
        ShowConversation(null);
        ApplyCaptureMode();
    }

    // Solo para CI: JACK_CAPTURE=empty|demo y JACK_THEME=light|dark preparan la ventana para la captura de pantalla.
    private void ApplyCaptureMode()
    {
        var mode = Environment.GetEnvironmentVariable("JACK_CAPTURE");
        if (mode is not ("empty" or "demo")) return;
        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 1000, 700));
        Root.RequestedTheme = Environment.GetEnvironmentVariable("JACK_THEME") == "dark" ? ElementTheme.Dark : ElementTheme.Light;
        if (mode != "demo") return;
        var demo = Path.Combine(Path.GetTempPath(), "jack-demo");
        (string Title, string Folder, ChatProvider Provider)[] samples =
        [
            ("Revisar el módulo de pagos", "tienda-web", ChatProvider.Codex),
            ("Migrar a .NET 10", "api-pedidos", ChatProvider.Claude),
            ("Documentar la CLI", "docs", ChatProvider.OpenCode)
        ];
        foreach (var (title, folder, provider) in samples)
            workspace.CreateConversation(title, Directory.CreateDirectory(Path.Combine(demo, folder)).FullName, provider);
        ConversationList.SelectedIndex = 0;
    }

    private void ShowConversation(ChatConversation? conversation)
    {
        EmptyState.Visibility = conversation is null ? Visibility.Visible : Visibility.Collapsed;
        ConversationView.Visibility = conversation is null ? Visibility.Collapsed : Visibility.Visible;
        if (conversation is null) return;
        HeaderPicture.DisplayName = conversation.ProviderTitle;
        ConversationTitle.Text = conversation.Title;
        ConversationDescription.Text = $"{conversation.ProviderTitle} · {conversation.ProjectPath}";
        Composer.PlaceholderText = $"Escribe un mensaje para {conversation.ProviderTitle}…";
    }

    private async void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        NewConversationButton.IsEnabled = false;
        try
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
            var folder = await picker.PickSingleFolderAsync();
            if (folder is null) return;

            var title = new TextBox { Header = "Nombre", Text = "Nuevo agente", MaxLength = 200 };
            var provider = new ComboBox
            {
                Header = "Proveedor",
                ItemsSource = new[] { "Codex", "Claude Code", "OpenCode" },
                SelectedIndex = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var validation = new TextBlock { TextWrapping = TextWrapping.Wrap };
            AutomationProperties.SetLiveSetting(validation, Microsoft.UI.Xaml.Automation.Peers.AutomationLiveSetting.Polite);
            var fields = new StackPanel { Spacing = 16 };
            fields.Children.Add(title);
            fields.Children.Add(provider);
            fields.Children.Add(new TextBlock { Text = folder.Path, TextWrapping = TextWrapping.Wrap });
            fields.Children.Add(validation);
            var dialog = new ContentDialog
            {
                XamlRoot = Root.XamlRoot,
                Title = "Nuevo agente",
                Content = fields,
                PrimaryButtonText = "Crear conversación",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary
            };
            dialog.PrimaryButtonClick += (_, args) =>
            {
                try
                {
                    var selectedProvider = provider.SelectedIndex switch
                    {
                        1 => ChatProvider.Claude,
                        2 => ChatProvider.OpenCode,
                        _ => ChatProvider.Codex
                    };
                    ConversationList.SelectedItem = workspace.CreateConversation(title.Text, folder.Path, selectedProvider);
                }
                catch (ArgumentException)
                {
                    validation.Text = "Indica un nombre y comprueba que la carpeta del proyecto siga disponible.";
                    args.Cancel = true;
                }
            };
            await dialog.ShowAsync();
        }
        catch (Exception error)
        {
            System.Diagnostics.Debug.WriteLine(error);
            await new ContentDialog
            {
                XamlRoot = Root.XamlRoot,
                Title = "No se pudo preparar la conversación",
                Content = "Vuelve a intentarlo y elige una carpeta local a la que tengas acceso.",
                CloseButtonText = "Cerrar"
            }.ShowAsync();
        }
        finally
        {
            NewConversationButton.IsEnabled = true;
        }
    }

    private void ConversationList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowConversation(ConversationList.SelectedItem as ChatConversation);
}
