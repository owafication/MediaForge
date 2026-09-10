using System.Windows;
using MediaForge.Models.Presets;

namespace MediaForge;

public partial class PresetEditorDialog : Window
{
    public PresetEditorDialog(PresetDocument? preset = null, string? suggestedName = null)
    {
        InitializeComponent();
        NameText.Text = suggestedName ?? preset?.Name ?? string.Empty;
        GroupText.Text = preset?.Group ?? "User";
        DescriptionText.Text = preset?.Description ?? string.Empty;
        LockedFieldsText.Text = preset is null ? string.Empty : string.Join(", ", preset.LockedFields);
    }

    public string PresetName => NameText.Text.Trim();
    public string PresetGroup => GroupText.Text.Trim();
    public string PresetDescription => DescriptionText.Text.Trim();
    public IReadOnlyList<string> LockedFields => LockedFieldsText.Text
        .Split([',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.Ordinal)
        .ToList();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PresetName))
        {
            MessageBox.Show(this, "Enter a preset name.", "Preset name required", MessageBoxButton.OK, MessageBoxImage.Warning);
            NameText.Focus();
            return;
        }
        if (PresetName.Length > 120 || PresetGroup.Length > 80 || PresetDescription.Length > 1000)
        {
            MessageBox.Show(this, "Preset metadata exceeds the supported length limits.", "Preset is too long", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var unknown = LockedFields.Where(field => !Services.Presets.PresetService.AllowedFieldKeys.Contains(field)).ToList();
        if (unknown.Count > 0)
        {
            MessageBox.Show(this, $"Unknown locked field key(s): {string.Join(", ", unknown)}", "Invalid locked fields", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }
}
