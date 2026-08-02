#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Model;

using WpfDisplay.Models;
using WpfDisplay.Services;

namespace WpfDisplay.ViewModels;

/// <summary>
/// Viewmodel for the palette editor page.
/// Manages key editing, color picking, save/cancel, and generator integration.
/// </summary>
public partial class PaletteEditorViewModel : ObservableObject
{
    private readonly PaletteLibraryService _library;
    private readonly MainViewModel _mainVm;
    private readonly Workspace _workspace;

    // The palette being edited (a working copy)
    public ColorPalette Palette { get; }

    // The collection this palette belongs to (null for new palettes not yet saved)
    public PaletteCollection? Collection { get; }

    // Whether this is a new palette (not yet in the library)
    public bool IsNew { get; }

    // Working copy of keys as viewmodels
    public ObservableCollection<ColorKeyViewModel> Keys { get; } = [];

    // Currently selected key
    [ObservableProperty]
    public partial ColorKeyViewModel? SelectedKey { get; set; }

    // Palette-level properties (bound to working copy)
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string TagsText { get; set; }

    [ObservableProperty]
    public partial InterpolationMode InterpolationMode { get; set; }

    [ObservableProperty]
    public partial bool HasBackgroundColor { get; set; }

    [ObservableProperty]
    public partial Color BackgroundColor { get; set; } = Colors.Black;

    // Collection name/description editing (left column)
    [ObservableProperty]
    public partial string CollectionName { get; set; }

    [ObservableProperty]
    public partial string CollectionDescription { get; set; }

    // Unsaved changes tracking
    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; set; }

    // Available interpolation modes for the dropdown
    public static InterpolationMode[] AllInterpolationModes { get; } = (InterpolationMode[])Enum.GetValues(typeof(InterpolationMode));

    // Collection palette list for left column context view
    public ObservableCollection<ColorPaletteViewModel> CollectionPalettes { get; } = [];

    // Callback to return to browser (set by PaletteManagerViewModel)
    public Action? OnReturnToBrowser { get; set; }

    /// <summary>
    /// Cancel editing and return to browser (prompts for save if unsaved).
    /// </summary>
    [RelayCommand]
    public async Task CancelAsync()
    {
        bool proceed = await PromptSaveAsync();
        if (proceed)
            OnReturnToBrowser?.Invoke();
    }

    public PaletteEditorViewModel() { }
    public PaletteEditorViewModel(PaletteLibraryService library, MainViewModel mainVm, ColorPalette palette, PaletteCollection? collection, bool isNew = false)
    {
        _library = library;
        _mainVm = mainVm;
        _workspace = mainVm.workspace;
        Palette = palette;
        Collection = collection;
        IsNew = isNew;

        // Initialize from palette
        Name = palette.Name;
        TagsText = string.Join(", ", palette.Tags);
        InterpolationMode = palette.InterpolationMode;
        HasBackgroundColor = palette.BackgroundColor.HasValue;
        BackgroundColor = HasBackgroundColor
            ? Color.FromRgb((byte)palette.BackgroundColor.Value.X, (byte)palette.BackgroundColor.Value.Y, (byte)palette.BackgroundColor.Value.Z)
            : Colors.Black;

        // Initialize collection info
        if (collection != null)
        {
            CollectionName = collection.Name;
            CollectionDescription = collection.Description;
        }

        // Build key viewmodels (SetEditorVm called after to avoid modifying palette during enumeration)
        foreach (var kvp in palette.KeyColors)
        {
            var keyVm = new ColorKeyViewModel(kvp.Key, kvp.Value);
            Keys.Add(keyVm);
        }
        foreach (var keyVm in Keys)
        {
            keyVm.SetEditorVm(this);
        }

        // Select first key
        if (Keys.Count > 0)
            SelectedKey = Keys[0];
    }

    partial void OnNameChanged(string value)
    {
        Palette.Name = value;
        MarkDirty();
    }

    partial void OnTagsTextChanged(string value)
    {
        Palette.Tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        MarkDirty();
    }

    partial void OnInterpolationModeChanged(InterpolationMode value)
    {
        Palette.InterpolationMode = value;
        MarkDirty();
    }

    partial void OnHasBackgroundColorChanged(bool value)
    {
        Palette.BackgroundColor = value ? Vector3.Create(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B) : null;
        MarkDirty();
    }

    partial void OnBackgroundColorChanged(Color value)
    {
        if (HasBackgroundColor)
        {
            Palette.BackgroundColor = Vector3.Create(value.R, value.G, value.B);
            MarkDirty();
        }
    }

    partial void OnCollectionNameChanged(string value)
    {
        if (Collection != null)
            Collection.Name = value;
    }

    partial void OnCollectionDescriptionChanged(string value)
    {
        if (Collection != null)
            Collection.Description = value;
    }

    private void MarkDirty()
    {
        HasUnsavedChanges = true;
    }

    /// <summary>
    /// Add a new key at the given position (or 0.5 if not specified).
    /// </summary>
    [RelayCommand]
    private void AddKey(double? position = null)
    {
        double pos = position ?? 0.5;
        pos = Math.Round(pos, 2);

        // Find nearest free position if occupied
        pos = ResolvePositionConflict(pos, -1);

        // Interpolate color from neighbors
        Vector4 color = InterpolateColorAt(pos);

        // Insert into sorted position
        Palette.KeyColors[pos] = color;
        RebuildKeys();
        MarkDirty();

        // Select the new key
        SelectedKey = Keys.FirstOrDefault(k => k.Position == pos);
    }

    /// <summary>
    /// Remove the selected key. Disabled if only 2 keys remain.
    /// </summary>
    public void RemoveKey(ColorKeyViewModel key)
    {
        if (Palette.KeyColors.Count <= 2)
            return;

        // Cannot remove keys at 0.0 or 1.0
        if (key.Position == 0.0 || key.Position == 1.0)
            return;

        Palette.KeyColors.Remove(key.Position);
        RebuildKeys();

        // Select a neighbor
        int index = Keys.IndexOf(key);
        Keys.Remove(key);
        if (Keys.Count > 0)
            SelectedKey = Keys[Math.Max(0, index - 1)];

        MarkDirty();
    }

    /// <summary>
    /// Duplicate the selected key at half-distance to the next key.
    /// </summary>
    public void DuplicateKey(ColorKeyViewModel key)
    {
        var positions = Palette.KeyColors.Keys.ToArray();
        int index = Array.IndexOf(positions, key.Position);

        double nextPos;
        if (index < positions.Length - 1)
            nextPos = positions[index + 1];
        else
            nextPos = 1.0;

        double newPos = Math.Round((key.Position + nextPos) * 0.5, 2);
        newPos = ResolvePositionConflict(newPos, -1);

        Palette.KeyColors[newPos] = key.Color.ToVector4();
        RebuildKeys();
        MarkDirty();

        // Select the new key
        SelectedKey = Keys.FirstOrDefault(k => k.Position == newPos);
    }

    /// <summary>
    /// Update the position of a key (from ValueSlider or drag).
    /// </summary>
    public void UpdateKeyPosition(ColorKeyViewModel key, double newPosition)
    {
        newPosition = Math.Round(Math.Clamp(newPosition, 0.0, 1.0), 2);
        if (newPosition == key.Position)
            return;

        // Resolve conflicts
        newPosition = ResolvePositionConflict(newPosition, key.Position);

        // Remove and re-insert at new position
        Vector4 color = key.Color.ToVector4();
        Palette.KeyColors.Remove(key.Position);
        Palette.KeyColors[newPosition] = color;
        key.Position = newPosition;
        RebuildKeys();
        MarkDirty();
    }

    /// <summary>
    /// Update the color of a key (from ColorPicker).
    /// </summary>
    public void UpdateKeyColor(ColorKeyViewModel key, Color newColor)
    {
        key.Color = newColor;
        Palette.KeyColors[key.Position] = newColor.ToVector4();
        MarkDirty();
    }

    /// <summary>
    /// Save the palette and return to browser.
    /// </summary>
    [RelayCommand]
    public async Task SaveAsync()
    {
        if (Collection != null)
        {
            if (IsNew)
                await _library.AddPaletteAsync(Collection.Id, Palette);
            else
                await _library.UpdatePaletteAsync(Palette, Collection.Id);
            await _library.SaveCollectionAsync(Collection);
        }
        HasUnsavedChanges = false;
        OnReturnToBrowser?.Invoke();
    }

    /// <summary>
    /// Check for unsaved changes and prompt user.
    /// Returns true if user chose to save or has no changes, false if cancelled.
    /// </summary>
    public async Task<bool> PromptSaveAsync()
    {
        if (!HasUnsavedChanges)
            return true;

        var result = MessageBox.Show(
            "Save changes to this palette?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return result switch
        {
            MessageBoxResult.Yes => await SaveAndReturn(true),
            MessageBoxResult.No => true,
            _ => false // Cancel
        };
    }

    private async Task<bool> SaveAndReturn(bool save)
    {
        if (save)
            await SaveAsync();
        return true;
    }

    #region Helpers

    private void RebuildKeys()
    {
        var oldSelected = SelectedKey;
        Keys.Clear();
        foreach (var kvp in Palette.KeyColors)
        {
            var keyVm = new ColorKeyViewModel(kvp.Key, kvp.Value);
            keyVm.SetEditorVm(this);
            Keys.Add(keyVm);
        }
        // Restore selection
        if (oldSelected != null)
            SelectedKey = Keys.FirstOrDefault(k => k.Position == oldSelected.Position);
        if (SelectedKey == null && Keys.Count > 0)
            SelectedKey = Keys[0];
    }

    private double ResolvePositionConflict(double position, double currentPos)
    {
        var existing = Palette.KeyColors.Keys.ToArray();

        // Try the position as-is, then step ±0.01 up to 50 times
        for (int i = 0; i < 50; i++)
        {
            bool conflict = false;
            foreach (var pos in existing)
            {
                if (Math.Abs(pos - position) < 0.005 && pos != currentPos)
                {
                    conflict = true;
                    break;
                }
            }
            if (!conflict)
                return position;

            // Step away: alternate direction each time
            double step = (i % 2 == 0 ? 0.01 : -0.01) * ((i / 2) + 1);
            position = Math.Round(Math.Clamp(position + step, 0.0, 1.0), 2);
        }

        // Fallback: find any free slot
        for (double p = 0.0; p <= 1.0; p += 0.01)
        {
            p = Math.Round(p, 2);
            bool conflict = false;
            foreach (var pos in existing)
            {
                if (Math.Abs(pos - p) < 0.005 && pos != currentPos)
                {
                    conflict = true;
                    break;
                }
            }
            if (!conflict)
                return p;
        }

        return position; // last resort
    }

    private Vector4 InterpolateColorAt(double position)
    {
        if (Palette.KeyColors.Count == 0)
            return new Vector4(1, 1, 1, 1);

        if (position <= Palette.KeyColors.Keys[0])
            return Palette.KeyColors.Values[0];
        if (position >= Palette.KeyColors.Keys[^1])
            return Palette.KeyColors.Values[^1];

        int i = Array.BinarySearch(Palette.KeyColors.Keys.ToArray(), position);
        if (i < 0)
            i = ~i;

        var pos1 = Palette.KeyColors.Keys[i - 1];
        var pos2 = Palette.KeyColors.Keys[i];
        var t = (float)((position - pos1) / (pos2 - pos1));

        return Vector4.Lerp(Palette.KeyColors.Values[i - 1], Palette.KeyColors.Values[i], t);
    }

    #endregion
}

/// <summary>
/// Extension methods for Color <-> Vector4 conversion.
/// </summary>
internal static class ColorVectorExtensions
{
    public static System.Numerics.Vector4 ToVector4(this Color color)
    {
        return new System.Numerics.Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
    }
}
