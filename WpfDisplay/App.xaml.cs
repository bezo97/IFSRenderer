using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfDisplay.Helper;

namespace WpfDisplay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string OpenVerbPath { get; private set; }

        public App()
        {
            NvOptimusHelper.InitializeDedicatedGraphics();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                OpenVerbPath = e.Args[0];
            }
        }
    }
}
