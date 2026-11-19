using Jack.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace Jack.WinUI;

// Piezas visuales que XAML no da hechas: colores de proveedor y halos de luz.
public static class Look
{
    private static Color Hex(uint rgb, byte alpha = 255) => Color.FromArgb(alpha, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

    private static LinearGradientBrush Diagonal(uint from, uint to) => new()
    {
        StartPoint = new Point(0, 0),
        EndPoint = new Point(1, 1),
        GradientStops = { new GradientStop { Color = Hex(from), Offset = 0 }, new GradientStop { Color = Hex(to), Offset = 1 } }
    };

    public static Brush Tile(ChatProvider provider) => provider switch
    {
        ChatProvider.Claude => Diagonal(0xF0956F, 0xC4553A),
        ChatProvider.OpenCode => Diagonal(0x6FA0FF, 0x3B5BDB),
        _ => Diagonal(0x34D399, 0x0F8F6B)
    };

    public static string Initials(ChatProvider provider) => provider switch
    {
        ChatProvider.Claude => "CC",
        ChatProvider.OpenCode => "OC",
        _ => "CX"
    };

    // Halo radial hecho con elipses concéntricas translúcidas: WinUI 3 no trae RadialGradientBrush.
    public static Grid Glow(uint rgb, double diameter, double alphaPerStep, int steps = 18)
    {
        var host = new Grid { Width = diameter, Height = diameter, IsHitTestVisible = false };
        for (var i = 0; i < steps; i++)
        {
            var size = diameter * Math.Sqrt(1 - (double)i / steps);
            host.Children.Add(new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Hex(rgb, (byte)Math.Round(alphaPerStep * 255)))
            });
        }
        return host;
    }
}

public enum TurnKind { User, Agent, Tool, Diff }

public sealed record DiffLine(char Sign, string Text)
{
    private static readonly SolidColorBrush Add = new(Color.FromArgb(0x2A, 0x2F, 0xBF, 0x71));
    private static readonly SolidColorBrush Del = new(Color.FromArgb(0x2A, 0xF0, 0x55, 0x5A));
    private static readonly SolidColorBrush None = new(Color.FromArgb(0, 0, 0, 0));

    public string Mark => Sign == ' ' ? "" : Sign.ToString();
    public Brush Tint => Sign switch { '+' => Add, '-' => Del, _ => None };
}

// Mensaje de la conversación. Solo la vista de demostración de CI los crea: todavía no hay conexión con las CLI.
public sealed record Turn(TurnKind Kind, ChatProvider Provider, string Text, string Detail = "", bool Running = false, IReadOnlyList<DiffLine>? Lines = null)
{
    public Visibility ShowRunning => Running ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ShowDone => Running ? Visibility.Collapsed : Visibility.Visible;
    public string ProviderTitle => Provider switch { ChatProvider.Claude => "Claude Code", ChatProvider.OpenCode => "OpenCode", _ => "Codex" };
}

public sealed partial class TurnSelector : DataTemplateSelector
{
    public DataTemplate? User { get; set; }
    public DataTemplate? Agent { get; set; }
    public DataTemplate? Tool { get; set; }
    public DataTemplate? Diff { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) => (item as Turn)?.Kind switch
    {
        TurnKind.User => User,
        TurnKind.Agent => Agent,
        TurnKind.Tool => Tool,
        TurnKind.Diff => Diff,
        _ => null
    };

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
}
