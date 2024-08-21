using System.Collections.Generic;
using System.Windows;

using CommunityToolkit.Mvvm.Input;

using IFSEngine.Model;

namespace WpfDisplay.Views;

//TODO: use or remove
//public partial class PaletteDialogWindow : Window
//{
//    public PaletteDialogWindow()
//    {
//        InitializeComponent();
//    }

//    public FlamePalette SelectedPalette { get; set; }

//    public List<FlamePalette> Palettes
//    {
//        get => (List<FlamePalette>)GetValue(PalettesProperty);
//        set => SetValue(PalettesProperty, value);
//    }
//    public static readonly DependencyProperty PalettesProperty =
//        DependencyProperty.Register("Palettes", typeof(List<FlamePalette>), typeof(PaletteDialogWindow), new PropertyMetadata(null));

//    private RelayCommand<FlamePalette> _paletteSelectedCommand;
//    public RelayCommand<FlamePalette> PaletteSelectedCommand =>
//    _paletteSelectedCommand ??= new RelayCommand<FlamePalette>((FlamePalette selected) =>
//    {
//        SelectedPalette = selected;
//        DialogResult = true;//closes the dialog
//                            //Close();
//    });

//    private RelayCommand _cancelDialogCommand;
//    public RelayCommand CancelDialogCommand =>
//    _cancelDialogCommand ??= new RelayCommand(() =>
//    {
//        SelectedPalette = null;
//        DialogResult = false;//closes the dialog
//                             //Close();
//    });

//}
