using IFSEngine;
using IFSEngine.Model;
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
                this.DataContext = renderer;

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

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            renderer.CurrentParams.SaveJson("tmp.json");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            renderer.CurrentParams = IFS.LoadJson("tmp.json");
            renderer.ActiveView = renderer.CurrentParams.Views.First();
            //renderer.ActiveView.Camera.OnManipulate += renderer.InvalidateAccumulation;//ez igy bena
            renderer.InvalidateParams();
            //szebb lenne pl.
            //renderer.LoadParams(IFS.LoadJson("tmp.json"), [ActiveView=0]);
        }

        int nextviewcnt = 1;
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            renderer.ActiveView = renderer.CurrentParams.Views[nextviewcnt++ % renderer.CurrentParams.Views.Count];
            renderer.InvalidateAccumulation();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            renderer.CurrentParams.Palette = UFPalette.FromFile(@"Resources\example.gradient")[0];
            renderer.InvalidateParams();
        }

    }
}
