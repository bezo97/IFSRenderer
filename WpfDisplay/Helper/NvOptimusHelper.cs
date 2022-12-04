using System;

namespace WpfDisplay.Helper;

//Force hardware acceleration for Nvidia Optimus laptops
//https://stackoverflow.com/questions/17270429/forcing-hardware-accelerated-rendering
public static partial class NvOptimusHelper
{
    [System.Runtime.InteropServices.LibraryImport("nvapi64.dll", EntryPoint = "fake")]
    private static partial int LoadNvApi64();

    [System.Runtime.InteropServices.LibraryImport("nvapi.dll", EntryPoint = "fake")]
    private static partial int LoadNvApi32();

    //To be called before first window creation
    public static void InitializeDedicatedGraphics()
    {
        try
        {
            if (Environment.Is64BitProcess)
                _ = LoadNvApi64();
            else
                _ = LoadNvApi32();
        }
        catch (DllNotFoundException) { } //not nvidia, ignore
        catch (EntryPointNotFoundException) { } //will always fail since 'fake' entry point doesn't exist. This is expected.
    }
}
