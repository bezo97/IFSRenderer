using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDisplay.Helper
{
    //Force hardware acceleration for Nvidia Optimus laptops
    //https://stackoverflow.com/questions/17270429/forcing-hardware-accelerated-rendering
    public static class NvOptimusHelper
    {
        [System.Runtime.InteropServices.DllImport("nvapi64.dll", EntryPoint = "fake")]
        static extern int LoadNvApi64();

        [System.Runtime.InteropServices.DllImport("nvapi.dll", EntryPoint = "fake")]
        static extern int LoadNvApi32();

        //To be called before first window creation
        public static void InitializeDedicatedGraphics()
        {
            try
            {
                if (Environment.Is64BitProcess)
                    LoadNvApi64();
                else
                    LoadNvApi32();
            }
            catch { } // will always fail since 'fake' entry point doesn't exists
        }
    }
}
