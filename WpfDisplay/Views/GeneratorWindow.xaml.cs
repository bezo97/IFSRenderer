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
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for GeneratorWindow.xaml
    /// </summary>
    public partial class GeneratorWindow : Window
    {


        public GeneratorWindow()
        {
            InitializeComponent();

            ContentRendered += (s, e) => {
                GeneratorViewModel vm = (GeneratorViewModel)DataContext;
                vm.ProcessQueue();
            };

        }
    }
}
