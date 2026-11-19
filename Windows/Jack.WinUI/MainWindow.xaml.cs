using System.Collections.Specialized;
using System.Numerics;
using Jack.Core;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.Storage.Pickers;

namespace Jack.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly ChatWorkspace workspace = new();
    private readonly Dictionary<Guid, IReadOnlyList<Turn>> demoTurns = [];

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        ConversationList.ItemsSource = workspace.Conversations;
        ((INotifyCollectionChanged)workspace.Conversations).CollectionChanged += (_, _) => AgentCount.Text = workspace.Conversations.Count.ToString();
        BuildAurora();
        TileCodex.Background = Look.Tile(ChatProvider.Codex);
        TileClaude.Background = Look.Tile(ChatProvider.Claude);
        TileOpenCode.Background = Look.Tile(ChatProvider.OpenCode);
        Breathe(RingOuter, 1.1f, 4.6);
        Breathe(RingInner, 1.07f, 3.4);
        FitToScreen();
        ShowConversation(null);
        ApplyCaptureMode();
    }

    // Los halos se ven sobre el lienzo oscuro; en claro se atenúan para que no ensucien el fondo.
    private void BuildAurora()
    {
        void Add(uint rgb, double size, double alpha, HorizontalAlignment h, VerticalAlignment v, Thickness margin)
        {
            var glow = Look.Glow(rgb, size, alpha);
            glow.HorizontalAlignment = h;
            glow.VerticalAlignment = v;
            glow.Margin = margin;
            Aurora.Children.Add(glow);
        }
        Add(0x6A5CFF, 980, 0.012, HorizontalAlignment.Left, VerticalAlignment.Top, new Thickness(-380, -420, 0, 0));
        Add(0xFF6A2A, 860, 0.011, HorizontalAlignment.Right, VerticalAlignment.Bottom, new Thickness(0, 0, -300, -380));
        Add(0x1E9BFF, 560, 0.009, HorizontalAlignment.Right, VerticalAlignment.Top, new Thickness(0, -260, 160, 0));
        void Dim() => Aurora.Opacity = Root.ActualTheme == ElementTheme.Light ? 0.5 : 1;
        Root.ActualThemeChanged += (_, _) => Dim();
        Root.Loaded += (_, _) => Dim();
    }

    // Respiración lenta de los anillos del estado vacío.
    private static void Breathe(FrameworkElement element, float peak, double seconds)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        element.SizeChanged += (_, e) => visual.CenterPoint = new Vector3((float)e.NewSize.Width / 2, (float)e.NewSize.Height / 2, 0);
        var ease = visual.Compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0), new Vector2(0.6f, 1));
        var animation = visual.Compositor.CreateVector3KeyFrameAnimation();
        animation.InsertKeyFrame(0, Vector3.One);
        animation.InsertKeyFrame(0.5f, new Vector3(peak, peak, 1), ease);
        animation.InsertKeyFrame(1, Vector3.One, ease);
        animation.Duration = TimeSpan.FromSeconds(seconds);
        animation.IterationBehavior = AnimationIterationBehavior.Forever;
        visual.StartAnimation("Scale", animation);
    }

    // El tamaño por defecto no debe superar el área útil (p. ej. una pantalla de 800x600).
    private void FitToScreen()
    {
        var work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var width = Math.Min(1100, work.Width);
        var height = Math.Min(740, work.Height);
        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(work.X + (work.Width - width) / 2, work.Y + (work.Height - height) / 2, width, height));
    }

    // Solo para CI: JACK_CAPTURE=empty|demo y JACK_THEME=light|dark preparan la ventana para la captura de pantalla.
    // "demo" rellena conversaciones y mensajes de ejemplo; la app real no los crea.
    private void ApplyCaptureMode()
    {
        var mode = Environment.GetEnvironmentVariable("JACK_CAPTURE");
        if (mode is not ("empty" or "demo")) return;
        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 1000, 700));
        Root.RequestedTheme = Environment.GetEnvironmentVariable("JACK_THEME") == "light" ? ElementTheme.Light : ElementTheme.Dark;
        if (mode != "demo") return;
        var demo = Path.Combine(Path.GetTempPath(), "jack-demo");
        ChatConversation Add(string title, string folder, ChatProvider provider) =>
            workspace.CreateConversation(title, Directory.CreateDirectory(Path.Combine(demo, folder)).FullName, provider);
        var pagos = Add("Revisar el módulo de pagos", "tienda-web", ChatProvider.Codex);
        var migrar = Add("Migrar a .NET 10", "api-pedidos", ChatProvider.Claude);
        var docs = Add("Documentar la CLI", "docs", ChatProvider.OpenCode);
        demoTurns[pagos.Id] =
        [
            new(TurnKind.User, pagos.Provider, "Revisa el módulo de pagos: los totales con descuento salen con un céntimo de diferencia."),
            new(TurnKind.Agent, pagos.Provider, "Voy a empezar por ver cómo se calcula el total y en qué punto se redondea."),
            new(TurnKind.Tool, pagos.Provider, "Leer Totales.cs", "src/Pagos/Totales.cs · 142 líneas"),
            new(TurnKind.Tool, pagos.Provider, "Buscar Math.Round", "6 coincidencias en 3 archivos"),
            new(TurnKind.Agent, pagos.Provider, "El descuento se redondea antes de sumar los impuestos y el error se acumula. Redondeo una sola vez, al final:"),
            new(TurnKind.Diff, pagos.Provider, "src/Pagos/Totales.cs", "+2  −2",
                Lines:
                [
                    new(' ', "var bruto = lineas.Sum(l => l.Precio * l.Cantidad);"),
                    new('-', "var descuento = Math.Round(bruto * tasa, 2);"),
                    new('-', "return Math.Round(bruto - descuento + impuestos, 2);"),
                    new('+', "var descuento = bruto * tasa;"),
                    new('+', "return Math.Round(bruto - descuento + impuestos, 2, MidpointRounding.AwayFromZero);")
                ]),
            new(TurnKind.Tool, pagos.Provider, "Ejecutar pruebas", "dotnet test --filter Pagos", Running: true)
        ];
        demoTurns[migrar.Id] =
        [
            new(TurnKind.User, migrar.Provider, "Migra api-pedidos a .NET 10 sin romper los contratos públicos."),
            new(TurnKind.Agent, migrar.Provider, "Empiezo por el archivo de proyecto y los paquetes que ya tienen versión compatible."),
            new(TurnKind.Tool, migrar.Provider, "Analizar dependencias", "12 paquetes · 2 requieren actualización", Running: true)
        ];
        demoTurns[docs.Id] =
        [
            new(TurnKind.User, docs.Provider, "Documenta los comandos de la CLI en docs/uso.md."),
            new(TurnKind.Agent, docs.Provider, "Listo el índice. Reviso cada comando con su ayuda integrada antes de describirlo.")
        ];
        ConversationList.SelectedIndex = 0;
    }

    private void ShowConversation(ChatConversation? conversation)
    {
        EmptyState.Visibility = conversation is null ? Visibility.Visible : Visibility.Collapsed;
        ConversationView.Visibility = conversation is null ? Visibility.Collapsed : Visibility.Visible;
        StatusText.Text = conversation is null ? "Sin agente seleccionado" : $"{conversation.ProviderTitle} · CLI sin conectar";
        StatusPath.Text = conversation?.ProjectPath ?? "";
        if (conversation is null) return;
        HeaderTile.Background = ComposerTile.Background = Look.Tile(conversation.Provider);
        HeaderInitials.Text = ComposerInitials.Text = Look.Initials(conversation.Provider);
        ConversationTitle.Text = conversation.Title;
        ConversationProvider.Text = ComposerProvider.Text = conversation.ProviderTitle;
        ConversationPath.Text = conversation.ProjectPath;
        ComposerFolder.Text = conversation.ProjectName;
        Composer.PlaceholderText = $"Escribe un mensaje para {conversation.ProviderTitle}…";
        demoTurns.TryGetValue(conversation.Id, out var turns);
        Transcript.ItemsSource = turns;
        ReadyCard.Visibility = turns is null ? Visibility.Visible : Visibility.Collapsed;
        // Como en un chat, lo más reciente queda a la vista.
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            TranscriptScroll.UpdateLayout();
            TranscriptScroll.ChangeView(null, TranscriptScroll.ScrollableHeight, null, true);
        });
    }

    private bool isCreating;

    private async void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        // Hay dos botones "Nuevo agente"; WinUI solo permite un ContentDialog abierto a la vez.
        if (isCreating) return;
        isCreating = true;
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
            isCreating = false;
        }
    }

    private void ConversationList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowConversation(ConversationList.SelectedItem as ChatConversation);
}
