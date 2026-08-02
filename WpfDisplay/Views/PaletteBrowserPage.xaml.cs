#nullable enable
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Palette browser page with two columns: browser area and preview panel.
/// </summary>
public partial class PaletteBrowserPage : Page
{
    public PaletteBrowserViewModel ViewModel => (PaletteBrowserViewModel)DataContext!;

    public PaletteBrowserPage()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            if (DataContext is PaletteBrowserViewModel vm)
            {
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(vm.GroupByCollection))
                        UpdateVisibility();
                    if (e.PropertyName == nameof(vm.HasFavorites) && vm.HasFavorites)
                        FavoritesExpander.IsExpanded = true;
                };
                // Initial state: expand favorites if any exist
                FavoritesExpander.IsExpanded = vm.HasFavorites;
            }
            UpdateVisibility();
        };
    }

    private void UpdateVisibility()
    {
        var vm = ViewModel;
        if (vm == null) return;

        // Grouped vs flat view
        if (vm.GroupByCollection)
        {
            GroupedView.Visibility = Visibility.Visible;
            FlatView.Visibility = Visibility.Collapsed;
        }
        else
        {
            GroupedView.Visibility = Visibility.Collapsed;
            FlatView.Visibility = Visibility.Visible;
        }
    }

    private void Swatch_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is PaletteSwatch swatch && swatch.PaletteVm != null)
        {
            ViewModel.SelectPaletteCommand.Execute(swatch.PaletteVm);
        }
    }

    private void Swatch_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is PaletteSwatch swatch && swatch.PaletteVm != null)
        {
            // Apply palette and close window (fast path)
            ViewModel.ApplyToFractalCommand.Execute(null);

            var window = Window.GetWindow(this);
            if (window != null)
                window.Close();
        }
    }

    private PaletteCollectionViewModel? _selectedCollectionVm;

    private void CollectionOptionsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is PaletteCollectionViewModel cvm)
        {
            _selectedCollectionVm = cvm;
            btn.ContextMenu.IsOpen = true;
        }
    }

    private void RenameCollection_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCollectionVm != null)
            ViewModel.RenameCollectionCommand.Execute(_selectedCollectionVm);
    }

    private void EditDescription_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCollectionVm != null)
            ViewModel.EditCollectionDescriptionCommand.Execute(_selectedCollectionVm);
    }

    private void ExportCollection_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCollectionVm != null)
            ViewModel.ExportCollectionCommand.Execute(_selectedCollectionVm);
    }

    private void DeleteCollection_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCollectionVm != null)
            ViewModel.DeleteCollectionCommand.Execute(_selectedCollectionVm);
    }

    private void CopyToButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            btn.Tag = ViewModel.FilteredCollections;
            btn.ContextMenu.IsOpen = true;
        }
    }

    private void CopyTo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item && item.DataContext is PaletteCollectionViewModel targetCvm
            && ViewModel.SelectedPalette != null)
        {
            ViewModel.CopyPaletteToCommand.Execute((ViewModel.SelectedPalette, targetCvm));
        }
    }
}
