using Jack.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Jack.WinUI;

// Dibuja un mensaje de la transcripción según su papel (Role): "user" en burbuja a la derecha, "assistant" a ancho
// completo con Markdown, "tool" como fila plegable, "jack" como aviso discreto y "error" en rojo.
public static class MessageView
{
    private static readonly Windows.UI.Color ErrorColor = Windows.UI.Color.FromArgb(255, 0xC4, 0x2B, 0x1C);

    public static FrameworkElement Build(ChatMessage message) => message.Role switch
    {
        "user" => User(message),
        "assistant" => Assistant(message),
        "tool" => Tool(message),
        "error" => Failure(message.Text),
        _ => Notice(message.Text)
    };

    private static FrameworkElement User(ChatMessage message)
    {
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(new TextBlock
        {
            Text = message.Text,
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true
        });
        if (message.Attachments is { Count: > 0 } files)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "Adjuntos: " + string.Join(", ", files.Select(path => Path.GetFileName(path))),
                Style = (Style)Application.Current.Resources["JackCaption"]
            });
        }
        return new Border
        {
            Style = (Style)Application.Current.Resources["JackBubble"],
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = 640,
            Child = stack
        };
    }

    private static FrameworkElement Assistant(ChatMessage message)
    {
        var stack = new StackPanel { Spacing = 10 };
        foreach (var block in MarkdownBlock.Parse(message.Text))
        {
            stack.Children.Add(MarkdownRenderer.Build(block));
        }
        return stack;
    }

    // Fila compacta: icono + título; si hay Detail, se despliega al pulsarla.
    private static FrameworkElement Tool(ChatMessage message)
    {
        var muted = (Brush)Application.Current.Resources["JackMutedBrush"];
        var hasDetail = !string.IsNullOrWhiteSpace(message.Detail);
        var header = new Grid { ColumnSpacing = 8, Padding = new Thickness(8, 4, 8, 4), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new FontIcon { Glyph = "", FontSize = 11, Foreground = muted };
        header.Children.Add(icon);

        var title = new TextBlock
        {
            Text = message.Text,
            FontSize = 12,
            Foreground = muted,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(title, 1);
        header.Children.Add(title);

        var chevron = new FontIcon { Glyph = "", FontSize = 9, Foreground = muted, VerticalAlignment = VerticalAlignment.Center, Visibility = hasDetail ? Visibility.Visible : Visibility.Collapsed };
        Grid.SetColumn(chevron, 2);
        header.Children.Add(chevron);

        var outer = new Border { CornerRadius = new CornerRadius(6), Child = header };
        if (!hasDetail) return outer;

        var detail = new Border
        {
            Style = (Style)Application.Current.Resources["JackCodeBlock"],
            Margin = new Thickness(22, 0, 0, 4),
            Visibility = Visibility.Collapsed,
            Child = new ScrollViewer
            {
                MaxHeight = 280,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = new TextBlock
                {
                    Text = message.Detail,
                    Style = (Style)Application.Current.Resources["JackMono"],
                    FontSize = 12,
                    IsTextSelectionEnabled = true
                }
            }
        };
        header.Tapped += (_, _) =>
        {
            var open = detail.Visibility == Visibility.Visible;
            detail.Visibility = open ? Visibility.Collapsed : Visibility.Visible;
            chevron.Glyph = open ? "" : "";
        };
        header.PointerEntered += (_, _) => outer.Background = (Brush)Application.Current.Resources["JackHoverBrush"];
        header.PointerExited += (_, _) => outer.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        return new StackPanel { Children = { outer, detail } };
    }

    private static FrameworkElement Failure(string text) =>
        new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true,
            Foreground = new SolidColorBrush(ErrorColor)
        };

    private static FrameworkElement Notice(string text) =>
        new TextBlock
        {
            Text = text,
            Style = (Style)Application.Current.Resources["JackCaption"],
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 4)
        };
}
