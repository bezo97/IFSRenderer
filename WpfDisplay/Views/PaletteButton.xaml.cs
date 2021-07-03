using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for PaletteButton.xaml
    /// </summary>
    public partial class PaletteButton : Button
    {
        public PaletteButton()
        {
            InitializeComponent();
        }

        public FlamePalette Palette
        {
            get { return (FlamePalette)GetValue(PaletteProperty); }
            set { SetValue(PaletteProperty, value); }
        }
        public static readonly DependencyProperty PaletteProperty =
            DependencyProperty.Register("Palette", typeof(FlamePalette), typeof(PaletteButton), 
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnPalettePropertyChanged)));
        private static void OnPalettePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue != null)
                ((PaletteButton)sender).SetGradient((FlamePalette)e.NewValue);
        }

        private void SetGradient(FlamePalette fp)
        {
            gradientStops.Clear();
            for (int i = 0; i < fp.Colors.Count; i++)
            {
                gradientStops.Add(new GradientStop(
                    Color.FromRgb(
                        (byte)(fp.Colors[i].X * 255),
                        (byte)(fp.Colors[i].Y * 255),
                        (byte)(fp.Colors[i].Z * 255)),
                    i / (double)fp.Colors.Count));
            }
            
        }


    }
}
