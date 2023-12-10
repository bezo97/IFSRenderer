using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Threading;
using WpfDisplay.Helper;
using WpfDisplay.Serialization;
using WpfDisplay.ViewModels;
using WpfDisplay.Views;

namespace WpfDisplay;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static string OpenVerbPath { get; private set; }

#if INSTALLER
    public static string AppDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IFSRenderer");
    public static string VersionString => "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " (installed)";
#endif
#if PORTABLE
    public static string AppDataPath { get; } = AppDomain.CurrentDomain.BaseDirectory;
    public static string VersionString => "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " (portable)";
#endif

    public static string TransformsDirectoryPath { get; } = Path.Combine(AppDataPath, "Transforms");
    public static string TemplatesDirectoryPath { get; } = Path.Combine(AppDataPath, "Templates");

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        NvOptimusHelper.InitializeDedicatedGraphics();
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        if (e.Args.Length > 0)
        {
            OpenVerbPath = e.Args[0];
        }

        //Open URIs in <Hyperlink> tags
        EventManager.RegisterClassHandler(typeof(Hyperlink), Hyperlink.RequestNavigateEvent,
            new RequestNavigateEventHandler((s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(e.Uri.ToString())
                    {
                        UseShellExecute = true
                    });
                }
                catch (System.ComponentModel.Win32Exception) { } //broken url format
                }));

    }

    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleUnexpectedException(e.Exception.InnerException);
        e.SetObserved();
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs dispatcherException)
    {
        HandleUnexpectedException(dispatcherException.Exception);
        dispatcherException.Handled = true;
    }

    private static void HandleUnexpectedException(Exception ex)
    {
        try
        {
            string logFilePath = LogException(ex);
            var saveParams = ((ViewModels.MainViewModel)Application.Current.MainWindow.DataContext)?.workspace.Ifs;
            string recoveryFilePath = Path.Combine(AppDataPath, "recovery.ifsjson");
            IfsNodesSerializer.SaveJsonFile(saveParams, recoveryFilePath);
            MessageBox.Show(Application.Current.MainWindow, $"IFSRenderer unexpectedly crashed. Details:\r\n{logFilePath}\r\nA recovery file has been saved to\r\n{recoveryFilePath}");
        }
        finally
        {
            Environment.Exit(1);
        }
    }

    public static string LogException(Exception ex)
    {
        string logDirectoryPath = Path.Combine(AppDataPath, "Logs");
        Directory.CreateDirectory(logDirectoryPath);
        string logFilePath = Path.Combine(logDirectoryPath, $"log-{DateTime.Now:yyyyMMddTHHmmss}.txt");
        File.WriteAllText(logFilePath, ex.ToString());
        return logFilePath;
    }

}
