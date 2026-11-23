using Jack.Core;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Jack.WinUI;

// Barra lateral al estilo del Mac: "Nuevo agente", búsqueda y los agentes agrupados por proyecto
// (los fijados arriba). Los elementos se construyen en código y solo se reconstruyen si cambia algo visible.
public sealed partial class SidebarView : UserControl
{
    private ChatStore? store;
    private string signature = "";
    private readonly HashSet<string> collapsed = new();
    private string query = "";

    /// <summary>El usuario pide un agente nuevo (botón o atajo).</summary>
    public event Action? NewAgentRequested;

    public SidebarView()
    {
        InitializeComponent();
    }

    public void Attach(ChatStore chatStore) => store = chatStore;

    private void NewAgentButton_Click(object sender, RoutedEventArgs e) => NewAgentRequested?.Invoke();

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        query = SearchBox.Text.Trim();
        signature = "";
        Refresh();
    }

    private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

    private static Brush Clear() => new SolidColorBrush(Microsoft.UI.Colors.Transparent);

    /// <summary>Repinta la lista si cambió lo que se ve.</summary>
    public void Refresh()
    {
        if (store is null) return;
        var rows = ChatPresentation.OrderedConversations(store)
            .Where(c => query.Length == 0
                || c.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || c.ProjectPath.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        var minute = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute;
        var key = store.SelectedId + "#" + minute + "#" + string.Join(",", collapsed) + "\n" + string.Join("\n", rows.Select(c =>
            $"{c.Id}|{c.Title}|{c.ProjectPath}|{ChatPresentation.StatusOf(store, c.Id).Label}|{c.HasUnread == true}|{c.PinnedAt is not null}"));
        if (key == signature) return;
        signature = key;

        ListHost.Children.Clear();
        if (rows.Count == 0)
        {
            ListHost.Children.Add(new TextBlock
            {
                Text = query.Length > 0 ? $"Sin resultados para «{query}»" : "Aún no hay agentes",
                Style = (Style)Application.Current.Resources["JackCaption"],
                Margin = new Thickness(8, 12, 8, 0)
            });
            return;
        }

        var pinned = rows.Where(c => c.PinnedAt is not null).ToList();
        if (pinned.Count > 0) AddGroup("Fijados", "\u0001pin", pinned, "\uE840");
        foreach (var group in rows.Where(c => c.PinnedAt is null).GroupBy(c => c.ProjectPath, StringComparer.OrdinalIgnoreCase))
        {
            AddGroup(ChatPresentation.ProjectName(group.Key), group.Key, group.ToList(), "\uE8B7");
        }
    }

    private void AddGroup(string title, string key, List<ChatConversation> items, string glyph)
    {
        var open = !collapsed.Contains(key) || query.Length > 0;
        var header = new Grid { ColumnSpacing = 8, Padding = new Thickness(8, 8, 8, 4), Background = Clear() };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var icon = new FontIcon { Glyph = glyph, FontSize = 12, Foreground = Res("JackMutedBrush") };
        Grid.SetColumn(icon, 0);
        header.Children.Add(icon);
        var name = new TextBlock
        {
            Text = title, FontSize = 12.5, FontWeight = FontWeights.Medium,
            TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = TextWrapping.NoWrap
        };
        Grid.SetColumn(name, 1);
        header.Children.Add(name);
        var count = new TextBlock { Text = open ? items.Count.ToString() : "▸ " + items.Count, FontSize = 11, Foreground = Res("JackFaintBrush") };
        Grid.SetColumn(count, 2);
        header.Children.Add(count);
        ToolTipService.SetToolTip(header, key.StartsWith('\u0001') ? "Fijados" : key);
        header.Tapped += (_, _) =>
        {
            if (!collapsed.Remove(key)) collapsed.Add(key);
            signature = "";
            Refresh();
        };
        ListHost.Children.Add(header);
        if (!open) return;
        foreach (var conversation in items) ListHost.Children.Add(BuildRow(conversation));
    }

    private static string Age(DateTime updatedUtc)
    {
        var span = DateTime.UtcNow - updatedUtc;
        if (span.TotalMinutes < 1) return "ahora";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} min";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours} h";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays} d";
        return $"{(int)(span.TotalDays / 30)} m";
    }

    private FrameworkElement BuildRow(ChatConversation conversation)
    {
        var (statusLabel, statusColor) = ChatPresentation.StatusOf(store!, conversation.Id);
        var unread = conversation.HasUnread == true;
        var selected = store!.SelectedId == conversation.Id;

        var grid = new Grid { ColumnSpacing = 8, Padding = new Thickness(8, 6, 8, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        if (statusLabel == "Inactivo")
        {
            var glyph = new FontIcon { Glyph = "\uE8BD", FontSize = 11, Foreground = Res("JackFaintBrush") };
            Grid.SetColumn(glyph, 0);
            grid.Children.Add(glyph);
        }
        else
        {
            var dot = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(statusColor), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            ToolTipService.SetToolTip(dot, statusLabel);
            Grid.SetColumn(dot, 0);
            grid.Children.Add(dot);
        }

        var title = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(conversation.Title) ? "Nuevo agente" : conversation.Title,
            FontSize = 12.5, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis,
            FontWeight = unread ? FontWeights.SemiBold : FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(title, 1);
        grid.Children.Add(title);

        var tail = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        if (unread) tail.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = Res("AccentFillColorDefaultBrush") });
        tail.Children.Add(new TextBlock { Text = Age(conversation.UpdatedAt), FontSize = 10.5, Foreground = Res("JackFaintBrush") });
        Grid.SetColumn(tail, 2);
        grid.Children.Add(tail);

        var row = new Border
        {
            CornerRadius = new CornerRadius(6),
            Background = selected ? Res("JackSelectionBrush") : Clear(),
            Child = grid,
            ContextFlyout = BuildMenu(conversation)
        };
        ToolTipService.SetToolTip(row, $"{conversation.Provider.Title()} · {conversation.ProjectPath}");
        if (!selected)
        {
            row.PointerEntered += (_, _) => row.Background = Res("JackHoverBrush");
            row.PointerExited += (_, _) => row.Background = Clear();
        }
        var id = conversation.Id;
        row.Tapped += (_, _) => { if (store!.SelectedId != id) store.Select(id); };
        return row;
    }

    // Menú contextual: los textos dependen del estado actual; la fila se reconstruye cuando ese estado cambia.
    private MenuFlyout BuildMenu(ChatConversation conversation)
    {
        var id = conversation.Id;
        var flyout = new MenuFlyout();
        flyout.Items.Add(Command("Renombrar", () => _ = RenameAsync(id)));
        flyout.Items.Add(Command(conversation.PinnedAt is null ? "Fijar" : "Desfijar", () => store?.SetPinned(id, !IsPinned(id))));
        flyout.Items.Add(Command(conversation.HasUnread == true ? "Marcar como leído" : "Marcar como no leído",
            () => store?.SetUnread(id, !IsUnread(id))));
        flyout.Items.Add(new MenuFlyoutSeparator());
        flyout.Items.Add(Command("Eliminar", () => _ = DeleteAsync(id)));
        return flyout;
    }

    private static MenuFlyoutItem Command(string text, Action action)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => action();
        return item;
    }

    private bool IsPinned(Guid id) => store?.Conversations.Any(conversation => conversation.Id == id && conversation.PinnedAt is not null) == true;

    private bool IsUnread(Guid id) => store?.Conversations.Any(conversation => conversation.Id == id && conversation.HasUnread == true) == true;

    private ChatConversation? Find(Guid id) => store?.Conversations.FirstOrDefault(conversation => conversation.Id == id);

    private async Task RenameAsync(Guid id)
    {
        var conversation = Find(id);
        if (store is null || conversation is null) return;
        var box = new TextBox
        {
            Text = conversation.Title,
            MaxLength = 120,
            PlaceholderText = "Nombre del agente"
        };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Renombrar agente",
            Content = box,
            PrimaryButtonText = "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary
        };
        if (await ChatPresentation.ShowDialogAsync(dialog) == ContentDialogResult.Primary) store.Rename(id, box.Text);
    }

    private async Task DeleteAsync(Guid id)
    {
        var conversation = Find(id);
        if (store is null || conversation is null) return;

        // El store no elimina una conversación mientras trabaja o tiene mensajes en cola.
        if (store.IsBusy(id))
        {
            await ChatPresentation.ShowDialogAsync(new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "No se puede eliminar",
                Content = "Espera a que el agente termine o deténlo antes de eliminarlo.",
                CloseButtonText = "Cerrar"
            });
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Eliminar agente",
            Content = $"Se eliminará «{conversation.Title}» y su historial. No se puede deshacer.",
            PrimaryButtonText = "Eliminar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close
        };
        if (await ChatPresentation.ShowDialogAsync(dialog) == ContentDialogResult.Primary) store.Remove(id);
    }
}
