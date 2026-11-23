using Jack.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;

namespace Jack.WinUI;

public sealed partial class MainWindow : Window
{
    // Borrador del compositor de una conversación: el texto y los archivos adjuntos aún no enviados.
    private sealed class Draft
    {
        public string Text = "";
        public List<string> Files = new();
    }

    private readonly ChatStore store;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer refreshTimer;
    private readonly Dictionary<Guid, Draft> drafts = new();

    private Guid? shownId;
    private Draft? draft;
    private Guid? comboOwner;
    private string effortKey = "";
    private string approvalKey = "";
    private bool sendShowsStop;
    private bool suppressCombos;
    private bool suppressErrorBar;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        try { AppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "jack.ico")); } catch { }
        SetTitleBar(AppTitleBar);
        FitToScreen();
        AppWindow.Changed += EnforceMinimumSize;
        ApplyCaptureTheme();

        // Un único store, creado en el hilo de interfaz: sus eventos llegan a este hilo.
        store = new ChatStore(new ChatArchive(), new JsonPreferences());
        refreshTimer = DispatcherQueue.CreateTimer();
        refreshTimer.Interval = TimeSpan.FromMilliseconds(50);
        refreshTimer.IsRepeating = false;
        refreshTimer.Tick += (_, _) => Refresh();
        store.Changed += ScheduleRefresh;

        Sidebar.Attach(store);
        Sidebar.NewAgentRequested += OpenNewAgentDialog;
        ChatPresentation.FitColumn(ApprovalsHost, ApprovalsBox);
        ChatPresentation.FitColumn(ComposerHost, ComposerBox, 780);
        Closed += (_, _) => store.Shutdown();
        SheetFolder.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Enter && SheetCreate.IsEnabled) { e.Handled = true; SheetCreate_Click(this, new RoutedEventArgs()); }
        };
        Root.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Escape && NewAgentSheet.Visibility == Visibility.Visible) { e.Handled = true; CloseNewAgentSheet(); }
        };
        Root.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Escape && NewAgentSheet.Visibility == Visibility.Visible) { e.Handled = true; CloseNewAgentSheet(); }
        };

        Refresh();
    }

    // ----------------------------------------------------------------------------------- Ventana

    // Tamaño inicial 1280x800, sin superar el área útil de la pantalla.
    private void FitToScreen()
    {
        var work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var width = Math.Min(1280, work.Width);
        var height = Math.Min(800, work.Height);
        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(work.X + (work.Width - width) / 2, work.Y + (work.Height - height) / 2, width, height));
    }

    // WinUI no tiene MinWidth en Window: si la ventana se hace más pequeña que 900x600, se vuelve a estirar.
    private void EnforceMinimumSize(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (!args.DidSizeChange) return;
        var size = sender.Size;
        var width = Math.Max(size.Width, 900);
        var height = Math.Max(size.Height, 600);
        if (width != size.Width || height != size.Height) sender.Resize(new Windows.Graphics.SizeInt32(width, height));
    }

    // Solo para capturas de CI: JACK_THEME=light|dark fuerza el tema de la ventana.
    private void ApplyCaptureTheme()
    {
        var theme = Environment.GetEnvironmentVariable("JACK_THEME");
        if (theme is "light" or "dark") Root.RequestedTheme = theme == "dark" ? ElementTheme.Dark : ElementTheme.Light;
    }

    private void NewAgentShortcut_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        OpenNewAgentDialog();
        args.Handled = true;
    }

    // ----------------------------------------------------------------------------------- Refresco

    // El store avisa en cada cambio pequeño (p. ej. cada fragmento de respuesta); agrupamos los repintados.
    private void ScheduleRefresh()
    {
        if (!refreshTimer.IsRunning) refreshTimer.Start();
    }

    private void Refresh()
    {
        Sidebar.Refresh();
        ShowError();

        var conversation = store.SelectedConversation;
        if (conversation is null)
        {
            shownId = null;
            draft = null;
            ShowEmpty();
            return;
        }

        EmptyState.Visibility = Visibility.Collapsed;
        ConversationView.Visibility = Visibility.Visible;
        if (conversation.Id != shownId) SwitchTo(conversation);

        var id = conversation.Id;
        ConversationTitle.Text = string.IsNullOrWhiteSpace(conversation.Title) ? "Nuevo agente" : conversation.Title;
        ConversationDescription.Text = $"{ChatPresentation.ProjectName(conversation.ProjectPath)} · {conversation.Provider.Title()}";

        var busy = store.IsBusy(id);
        var working = store.Statuses.TryGetValue(id, out var status) && (status == ChatStatus.Running || status == ChatStatus.Queued);
        Transcript.Show(id, conversation.Messages, working);

        ShowApprovals(id);
        ShowQueue(id);
        RestoreRecalled(id);
        SyncSettings(conversation, busy);
        UpdateSendButton();
    }

    private void ShowEmpty()
    {
        var any = store.Conversations.Count > 0;
        EmptyTitle.Text = any ? "Selecciona un agente" : "Crea tu primer agente";
        EmptyBody.Text = any
            ? "Elige una conversación en la barra lateral o crea una nueva."
            : "Elige la carpeta de un proyecto y un proveedor para preparar una conversación.";
        ConversationTitle.Text = "";
        ConversationDescription.Text = "";
        EmptyState.Visibility = Visibility.Visible;
        ConversationView.Visibility = Visibility.Collapsed;
    }

    private void ShowError()
    {
        var message = store.ErrorMessage;
        suppressErrorBar = true;
        ErrorBar.Message = message ?? "";
        ErrorBar.IsOpen = message is not null;
        suppressErrorBar = false;
    }

    private void ErrorBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        if (!suppressErrorBar) store.DismissError();
    }

    // Cambia la conversación mostrada: guarda el borrador de la anterior y carga el de la nueva.
    private void SwitchTo(ChatConversation conversation)
    {
        if (shownId is { } previous && draft is not null) drafts[previous] = draft;
        shownId = conversation.Id;
        if (!drafts.TryGetValue(conversation.Id, out draft))
        {
            draft = new Draft();
            drafts[conversation.Id] = draft;
        }
        Composer.Text = draft.Text;
        RenderAttachments();
        comboOwner = null;
        effortKey = "";
        approvalKey = "";
        // El cursor queda listo en el compositor al abrir o crear un agente.
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => Composer.Focus(FocusState.Programmatic));
    }

    // ----------------------------------------------------------------------------------- Aprobaciones

    private void ShowApprovals(Guid id)
    {
        var approvals = store.Approvals.TryGetValue(id, out var list) ? list : null;
        // Solo se reconstruye si cambian las aprobaciones: así no se pierden las respuestas que el usuario escribe.
        var key = id + "|" + (approvals is null ? "" : string.Join("|", approvals.Select(approval => approval.Id + ":" + approval.Questions.Count)));
        if (key != approvalKey)
        {
            approvalKey = key;
            ApprovalsBox.Children.Clear();
            if (approvals is not null)
            {
                foreach (var approval in approvals) ApprovalsBox.Children.Add(ApprovalView.Build(store, id, approval));
            }
        }
        ApprovalsHost.Visibility = ApprovalsBox.Children.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    // ----------------------------------------------------------------------------------- Cola de mensajes y adjuntos

    private void ShowQueue(Guid id)
    {
        QueueHost.Children.Clear();
        if (store.Waiting.TryGetValue(id, out var waiting))
        {
            foreach (var message in waiting)
            {
                var text = new TextBlock
                {
                    Text = "En espera: " + ChatPresentation.Preview(message.Text, 48),
                    Style = (Style)Application.Current.Resources["JackCaption"],
                    TextWrapping = TextWrapping.NoWrap,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                QueueHost.Children.Add(new Border
                {
                    Style = (Style)Application.Current.Resources["JackChip"],
                    Child = text
                });
            }
        }
        QueueHost.Visibility = QueueHost.Children.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    // Si el agente se detuvo con mensajes en espera, el texto vuelve al compositor para editarlo.
    private void RestoreRecalled(Guid id)
    {
        if (draft is null || !store.Recalled.TryGetValue(id, out var recalled)) return;
        if (Composer.Text.Trim().Length == 0 && draft.Files.Count == 0)
        {
            Composer.Text = recalled.Text;
            draft.Files = recalled.Attachments.ToList();
            RenderAttachments();
        }
        store.ClearRecalled(id);
    }

    private void RenderAttachments()
    {
        AttachmentsHost.Children.Clear();
        var owner = draft;
        var files = owner?.Files ?? new List<string>();
        foreach (var path in files)
        {
            var chip = new Button
            {
                Content = Path.GetFileName(path) + "  ×",
                Padding = new Thickness(8, 2, 8, 2),
                MinHeight = 0
            };
            ToolTipService.SetToolTip(chip, path);
            chip.Click += (_, _) =>
            {
                owner?.Files.Remove(path);
                RenderAttachments();
                UpdateSendButton();
            };
            AttachmentsHost.Children.Add(chip);
        }
        AttachmentsHost.Visibility = files.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    // ----------------------------------------------------------------------------------- Ajustes (modelo, esfuerzo, modo)

    private void SyncSettings(ChatConversation conversation, bool busy)
    {
        suppressCombos = true;
        try
        {
            var provider = conversation.Provider;
            if (comboOwner != conversation.Id)
            {
                comboOwner = conversation.Id;
                ModeBox.Items.Clear();
                foreach (var mode in ChatRunMode.Choices(provider))
                {
                    ModeBox.Items.Add(new ComboBoxItem { Content = mode.Title, Tag = mode.Id });
                }
                ModelBox.Items.Clear();
                foreach (var choice in store.ModelChoices(provider))
                {
                    ModelBox.Items.Add(new ComboBoxItem { Content = choice.Title, Tag = choice.Id });
                }
            }

            // Modelo: si no está en la lista (p. ej. una versión antigua), se añade para que se vea.
            SelectByTag(ModelBox, conversation.Model, () => new ComboBoxItem
            {
                Content = ChatPresentation.ModelTitle(conversation.Model),
                Tag = conversation.Model
            });

            // Esfuerzo: depende del modelo, así que se reconstruye solo cuando cambia el modelo.
            var key = conversation.Id + "|" + conversation.Model;
            if (key != effortKey)
            {
                effortKey = key;
                EffortBox.Items.Clear();
                foreach (var effort in store.SupportedEfforts(provider, conversation.Model))
                {
                    EffortBox.Items.Add(new ComboBoxItem { Content = ChatModelChoice.EffortTitle(effort), Tag = effort });
                }
            }
            SelectByTag(EffortBox, conversation.Effort, () => new ComboBoxItem
            {
                Content = ChatModelChoice.EffortTitle(conversation.Effort),
                Tag = conversation.Effort
            });

            SelectByTag(ModeBox, conversation.Mode ?? "manual", () => new ComboBoxItem
            {
                Content = ChatPresentation.ModeTitle(conversation),
                Tag = conversation.Mode ?? "manual"
            });

            // Como en Swift, el modelo y el esfuerzo no cambian con un turno en marcha o con mensajes en cola.
            ModelBox.IsEnabled = !busy;
            EffortBox.IsEnabled = !busy;
            ModeBox.IsEnabled = !busy || store.ChangesModeLive(conversation.Id);
        }
        finally
        {
            suppressCombos = false;
        }
    }

    // Selecciona el elemento cuyo Tag coincide; si no existe, lo añade con <paramref name="create"/>.
    private static void SelectByTag(ComboBox box, string? tag, Func<ComboBoxItem> create)
    {
        if (tag is null) return;
        foreach (var item in box.Items.OfType<ComboBoxItem>())
        {
            if ((item.Tag as string) == tag)
            {
                box.SelectedItem = item;
                return;
            }
        }
        var added = create();
        box.Items.Add(added);
        box.SelectedItem = added;
    }

    private void ModelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressCombos || shownId is not { } id) return;
        if (ModelBox.SelectedItem is not ComboBoxItem { Tag: string model }) return;
        var conversation = store.SelectedConversation;
        if (conversation is null || model == conversation.Model) return;
        store.UpdateSettings(id, model, conversation.Effort);
    }

    private void EffortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressCombos || shownId is not { } id) return;
        if (EffortBox.SelectedItem is not ComboBoxItem { Tag: string effort }) return;
        var conversation = store.SelectedConversation;
        if (conversation is null || effort == conversation.Effort) return;
        store.UpdateSettings(id, conversation.Model, effort);
    }

    private void ModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressCombos || shownId is not { } id) return;
        if (ModeBox.SelectedItem is not ComboBoxItem { Tag: string mode }) return;
        var conversation = store.SelectedConversation;
        if (conversation is null || mode == conversation.Mode) return;
        store.UpdateMode(id, mode, ChatRunMode.Choices(conversation.Provider));
    }

    // ----------------------------------------------------------------------------------- Compositor

    // Mientras la conversación está ocupada y el compositor está vacío, el botón detiene al agente.
    private void UpdateSendButton()
    {
        var empty = Composer.Text.Trim().Length == 0 && (draft?.Files.Count ?? 0) == 0;
        var showStop = shownId is { } id && store.IsBusy(id) && empty;
        if (showStop == sendShowsStop) return;
        sendShowsStop = showStop;
        SendGlyph.Glyph = showStop ? "\uE71A" : "\uE74A";
        ToolTipService.SetToolTip(SendButton, showStop ? "Detener" : "Enviar");
        AutomationProperties.SetName(SendButton, showStop ? "Detener" : "Enviar");
    }

    private void Composer_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (draft is not null) draft.Text = Composer.Text;
        UpdateSendButton();
    }

    private void Composer_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter) return;
        // Mayús+Intro inserta una nueva línea; Intro envía.
        if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) return;
        e.Handled = true;
        SendCurrent();
    }

    private void Send_Click(object sender, RoutedEventArgs e)
    {
        if (shownId is not { } id || draft is null) return;
        var empty = Composer.Text.Trim().Length == 0 && draft.Files.Count == 0;
        // Ocupada y sin texto: el botón es "Detener". Con texto, se envía y el store lo pone en cola.
        if (empty && store.IsBusy(id))
        {
            store.Stop(id);
            return;
        }
        SendCurrent();
    }

    private void SendCurrent()
    {
        if (shownId is not { } id || draft is null) return;
        var text = Composer.Text;
        if (text.Trim().Length == 0 && draft.Files.Count == 0) return;
        var files = draft.Files.ToList();
        // Si el agente está ocupado, store.Send deja el mensaje en cola (store.Waiting) y lo entrega al terminar.
        store.Send(text, id, files.Count == 0 ? null : files);
        Composer.Text = "";
        draft.Files.Clear();
        RenderAttachments();
        UpdateSendButton();
    }

    private void Attach_Click(object sender, RoutedEventArgs e)
    {
        if (draft is null) return;
        try
        {
            foreach (var path in NativePicker.PickFiles(WinRT.Interop.WindowNative.GetWindowHandle(this), "Adjuntar archivos"))
            {
                if (!draft.Files.Contains(path)) draft.Files.Add(path);
            }
            RenderAttachments();
            UpdateSendButton();
        }
        catch (Exception error)
        {
            store.ReportError("No se pudo abrir el selector de archivos: " + error.Message);
        }
    }

    // ----------------------------------------------------------------------------------- Nuevo agente

    private void NewAgent_Click(object sender, RoutedEventArgs e) => OpenNewAgentDialog();

    private void OpenNewAgentDialog()
    {
        SheetFolder.Text = "";
        SheetName.Text = "";
        SheetHint.Visibility = Visibility.Collapsed;
        SheetCreate.IsEnabled = false;

        // Proyectos usados antes, el más reciente primero, como atajo.
        SheetRecent.Children.Clear();
        var recent = store.Conversations
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => c.ProjectPath)
            .Where(path => Directory.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
        foreach (var path in recent)
        {
            var row = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 5, 8, 5),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new FontIcon { Glyph = "\uE8B7", FontSize = 12 },
                        new TextBlock { Text = ChatPresentation.ProjectName(path), FontSize = 12.5 },
                        new TextBlock { Text = path, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis,
                                        Style = (Style)Application.Current.Resources["JackCaption"], MaxWidth = 280 }
                    }
                }
            };
            var chosen = path;
            row.Click += (_, _) => SheetFolder.Text = chosen;
            SheetRecent.Children.Add(row);
        }
        SheetRecentBox.Visibility = recent.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        NewAgentSheet.Visibility = Visibility.Visible;
        SheetFolder.Focus(FocusState.Programmatic);
    }

    private void CloseNewAgentSheet() => NewAgentSheet.Visibility = Visibility.Collapsed;

    private void SheetScrim_Tapped(object sender, TappedRoutedEventArgs e) => CloseNewAgentSheet();

    private void SheetCard_Tapped(object sender, TappedRoutedEventArgs e) => e.Handled = true;

    private void SheetCancel_Click(object sender, RoutedEventArgs e) => CloseNewAgentSheet();

    private void SheetFolder_TextChanged(object sender, TextChangedEventArgs e)
    {
        var path = SheetFolder.Text.Trim().Trim('"');
        var valid = path.Length > 0 && Directory.Exists(path);
        SheetCreate.IsEnabled = valid;
        SheetHint.Text = "La carpeta no existe.";
        SheetHint.Visibility = path.Length > 0 && !valid ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SheetBrowse_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = NativePicker.PickFolder(WinRT.Interop.WindowNative.GetWindowHandle(this), "Carpeta del proyecto");
            if (folder is not null) SheetFolder.Text = folder;
        }
        catch (Exception error)
        {
            SheetHint.Text = "No se pudo abrir el selector: " + error.Message + ". Escribe la ruta a mano.";
            SheetHint.Visibility = Visibility.Visible;
        }
    }

    private void SheetCreate_Click(object sender, RoutedEventArgs e)
    {
        var path = SheetFolder.Text.Trim().Trim('"');
        if (!Directory.Exists(path)) return;
        var name = string.IsNullOrWhiteSpace(SheetName.Text) ? null : SheetName.Text.Trim();
        CloseNewAgentSheet();
        // Create selecciona la conversación nueva; si algo falla, el store deja el aviso en ErrorMessage.
        store.Create(path, ChatProvider.Claude, title: name);
        Refresh();
    }
}
