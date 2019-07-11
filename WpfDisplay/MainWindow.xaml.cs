using IFSEngine;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;

namespace WpfDisplay
{
    //TODO: implement previously working functionality:
    //Buttons: Start/Stop render, Randomize, Save Image
    //Numeric: Brightness, Gamma, DOF, Fog, Focus Distance
    //Image saving

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RendererGL renderer;
        public MainWindow()
        {
            InitializeComponent();
            Host.Loaded += (s, e) =>
            {
                renderer = Host.Renderer;
                BrightnessSetter.Value = renderer.Brightness;
                GammaSetter.Value = renderer.Gamma;
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            renderer.StartRendering();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            renderer.Reset();
        }

        private void Brightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (renderer == null)
                return;
            renderer.Brightness =(float) BrightnessSetter.Value;
        }

        private void Gamma_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (renderer == null)
                return;
            renderer.Gamma = (float)GammaSetter.Value;
        }
    }
}
