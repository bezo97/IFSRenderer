using IFSEngine.Model;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfDisplay.Views
{
    public partial class PaletteDialogWindow : Window
    {
        public PaletteDialogWindow()
        {
            InitializeComponent();
        }

        public FlamePalette SelectedPalette { get; set; }

        public List<FlamePalette> Palettes
        {
            get => (List<FlamePalette>)GetValue(PalettesProperty);
            set => SetValue(PalettesProperty, value);
        }
        public static readonly DependencyProperty PalettesProperty =
            DependencyProperty.Register("Palettes", typeof(List<FlamePalette>), typeof(PaletteDialogWindow), new PropertyMetadata(null));

        private RelayCommand<FlamePalette> _paletteSelectedCommand;
        public RelayCommand<FlamePalette> PaletteSelectedCommand =>
        _paletteSelectedCommand ??= new RelayCommand<FlamePalette>((FlamePalette selected) =>
        {
            SelectedPalette = selected;
            DialogResult = true;//closes the dialog
            //Close();
        });

        private RelayCommand _cancelDialogCommand;
        public RelayCommand CancelDialogCommand =>
        _cancelDialogCommand ??= new RelayCommand(() =>
        {
            SelectedPalette = null;
            DialogResult = false;//closes the dialog
            //Close();
        });

    }
}
