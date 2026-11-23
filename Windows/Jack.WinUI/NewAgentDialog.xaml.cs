using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace Jack.WinUI;

// Diálogo "Nuevo agente": carpeta del proyecto, nombre opcional y proveedor. Solo Claude Code está disponible en Windows.
public sealed partial class NewAgentDialog : ContentDialog
{
    public NewAgentDialog()
    {
        InitializeComponent();
    }

    /// <summary>Ventana propietaria: el selector de carpetas de WinRT necesita su identificador.</summary>
    public IntPtr OwnerHandle { get; set; }

    /// <summary>Carpeta elegida, o null si no se ha elegido ninguna.</summary>
    public string? ProjectPath { get; private set; }

    /// <summary>Nombre escrito por el usuario, o null para usar el predeterminado.</summary>
    public string? AgentTitle => string.IsNullOrWhiteSpace(NameBox.Text) ? null : NameBox.Text.Trim();

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, OwnerHandle);
        var folder = await picker.PickSingleFolderAsync();
        if (folder is null) return;

        ProjectPath = folder.Path;
        FolderBox.Text = folder.Path;
        IsPrimaryButtonEnabled = true;
    }
}
